import json
import logging
import os
import threading
import time
from typing import Any, Dict, Optional

import requests

logger = logging.getLogger(__name__)

DEFAULT_BASE_URL = "http://localhost:11434"
DEFAULT_MODEL = "llama3.2:latest"
DEFAULT_TIMEOUT_SECONDS = 30

# Resource-safety limits: the LLM is an optional accelerator, never a requirement.
MAX_ALLOWED_ATTEMPTS = 3
DEFAULT_MAX_ATTEMPTS = 1
DEFAULT_MAX_CONCURRENT_CALLS = 1
DEFAULT_FAILURE_THRESHOLD = 3
DEFAULT_COOLDOWN_SECONDS = 120
_SEMAPHORE_ACQUIRE_TIMEOUT_SECONDS = 5


class OllamaClientError(Exception):
    pass


def _env_flag(name: str, default: bool) -> bool:
    raw = os.environ.get(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


class _CircuitBreaker:
    """Fail fast after repeated consecutive Ollama failures so a struggling local
    model is not hammered by every incoming request."""

    def __init__(self, threshold: int, cooldown_seconds: int):
        self._lock = threading.Lock()
        self._threshold = max(1, threshold)
        self._cooldown_seconds = cooldown_seconds
        self._consecutive_failures = 0
        self._opened_at = 0.0

    def is_open(self) -> tuple:
        with self._lock:
            if self._consecutive_failures < self._threshold:
                return False, 0.0
            elapsed = time.monotonic() - self._opened_at
            if elapsed >= self._cooldown_seconds:
                # Cooldown expired: allow one attempt through (half-open).
                self._consecutive_failures = self._threshold - 1
                return False, 0.0
            return True, self._cooldown_seconds - elapsed

    def record_success(self) -> None:
        with self._lock:
            self._consecutive_failures = 0

    def record_failure(self) -> None:
        with self._lock:
            self._consecutive_failures += 1
            if self._consecutive_failures >= self._threshold:
                if self._consecutive_failures == self._threshold:
                    self._opened_at = time.monotonic()
                    logger.warning(
                        "Ollama circuit breaker OPEN for %ss after %s consecutive failures.",
                        self._cooldown_seconds, self._threshold)


class OllamaClient:
    def __init__(
        self,
        base_url: Optional[str] = None,
        model: Optional[str] = None,
        timeout_seconds: Optional[int] = None,
    ):
        self.base_url = (base_url or os.environ.get("OLLAMA_BASE_URL") or DEFAULT_BASE_URL).rstrip("/")
        self.model = model or os.environ.get("OLLAMA_MODEL") or DEFAULT_MODEL
        self.timeout_seconds = timeout_seconds or int(os.environ.get("OLLAMA_TIMEOUT_SECONDS", DEFAULT_TIMEOUT_SECONDS))
        self.enabled = _env_flag("OLLAMA_ENABLED", True)
        self.max_attempts = min(
            MAX_ALLOWED_ATTEMPTS,
            max(1, int(os.environ.get("OLLAMA_MAX_ATTEMPTS", DEFAULT_MAX_ATTEMPTS))),
        )
        self.failure_threshold = max(1, int(os.environ.get("OLLAMA_FAILURE_THRESHOLD", DEFAULT_FAILURE_THRESHOLD)))
        self.cooldown_seconds = max(0, int(os.environ.get("OLLAMA_COOLDOWN_SECONDS", DEFAULT_COOLDOWN_SECONDS)))

    def generate_structured(
        self,
        system_prompt: str,
        user_prompt: str,
        expected_schema: Optional[type] = None,
    ) -> Dict[str, Any]:
        if not self.enabled:
            logger.info("Ollama disabled by configuration (OLLAMA_ENABLED=false); caller should use deterministic fallback.")
            return {
                "_ollama_error": "Ollama disabled by configuration (OLLAMA_ENABLED=false).",
                "_ollama_disabled": True,
                "_ollama_latency_ms": 0,
            }

        open_now, remaining = _circuit_breaker(self).is_open()
        if open_now:
            logger.warning("Ollama circuit breaker open; failing fast (cooldown %.0fs remaining).", remaining)
            return {
                "_ollama_error": "Ollama temporarily unavailable: circuit breaker open after repeated failures.",
                "_ollama_circuit_open": True,
                "_ollama_latency_ms": 0,
            }

        if not _ollama_semaphore().acquire(timeout=_SEMAPHORE_ACQUIRE_TIMEOUT_SECONDS):
            logger.warning("Ollama concurrency limit reached; failing fast instead of queuing another model call.")
            return {
                "_ollama_error": "Ollama is busy with another request; concurrency limit reached.",
                "_ollama_busy": True,
                "_ollama_latency_ms": 0,
            }

        try:
            return self._generate_structured_with_retries(system_prompt, user_prompt, expected_schema)
        finally:
            _ollama_semaphore().release()

    def _generate_structured_with_retries(
        self,
        system_prompt: str,
        user_prompt: str,
        expected_schema: Optional[type],
    ) -> Dict[str, Any]:
        # Bounded retries on transport failures only; parse/schema errors are
        # deterministic and retrying would just burn local model time.
        last_error: Optional[str] = None
        for attempt in range(1, self.max_attempts + 1):
            try:
                raw_text, latency_ms = self._call_generate(system_prompt, user_prompt)
            except OllamaClientError as exc:
                last_error = str(exc)
                logger.warning("Ollama call failed (attempt %s/%s): %s", attempt, self.max_attempts, exc)
                _circuit_breaker(self).record_failure()
                if attempt < self.max_attempts:
                    time.sleep(0.5)
                continue

            _circuit_breaker(self).record_success()
            parsed = self._extract_json(raw_text)
            parsed["_ollama_latency_ms"] = latency_ms

            if expected_schema is not None:
                parsed = self._validate_with_schema(parsed, expected_schema)

            return parsed

        return {"_ollama_error": last_error or "Ollama call failed.", "_ollama_latency_ms": 0}

    def is_available(self) -> bool:
        if not self.enabled:
            return False
        try:
            resp = requests.get(f"{self.base_url}/api/tags", timeout=5)
            return resp.status_code == 200
        except Exception:
            return False

    def _call_generate(self, system_prompt: str, user_prompt: str) -> tuple:
        url = f"{self.base_url}/api/generate"
        payload = {
            "model": self.model,
            "prompt": user_prompt,
            "system": system_prompt,
            "stream": False,
            "options": {
                "temperature": 0.1,
                "num_predict": 512,
            },
        }

        start = time.monotonic()
        try:
            resp = requests.post(url, json=payload, timeout=self.timeout_seconds)
        except requests.ConnectionError:
            raise OllamaClientError("Ollama is not running or not reachable at {}".format(self.base_url))
        except requests.Timeout:
            raise OllamaClientError("Ollama request timed out after {}s".format(self.timeout_seconds))
        except requests.RequestException as exc:
            raise OllamaClientError("Ollama request failed: {}".format(exc))
        latency_ms = int((time.monotonic() - start) * 1000)

        if resp.status_code != 200:
            raise OllamaClientError("Ollama returned HTTP {}".format(resp.status_code))

        try:
            data = resp.json()
        except (json.JSONDecodeError, ValueError):
            raise OllamaClientError("Ollama returned non-JSON response")

        raw_text = data.get("response", "")
        if not raw_text:
            raise OllamaClientError("Ollama returned empty response")

        return raw_text, latency_ms

    @staticmethod
    def _extract_json(raw_text: str) -> Dict[str, Any]:
        text = raw_text.strip()

        try:
            return json.loads(text)
        except (json.JSONDecodeError, ValueError):
            pass

        start = text.find("{")
        end = text.rfind("}")
        if start != -1 and end != -1 and end > start:
            try:
                return json.loads(text[start:end + 1])
            except (json.JSONDecodeError, ValueError):
                pass

        logger.warning("Ollama response could not be parsed as JSON: %s", text[:200])
        return {"_ollama_parse_error": True, "_ollama_raw_preview": text[:500]}

    @staticmethod
    def _validate_with_schema(data: Dict[str, Any], schema_cls: type) -> Dict[str, Any]:
        internal_keys = {
            "_ollama_error",
            "_ollama_latency_ms",
            "_ollama_parse_error",
            "_ollama_raw_preview",
            "_ollama_disabled",
            "_ollama_circuit_open",
            "_ollama_busy",
        }
        if any(k in data for k in internal_keys):
            return data

        try:
            validated = schema_cls(**data)
            return validated.model_dump()
        except Exception as exc:
            logger.warning("Ollama output failed schema validation: %s", exc)
            data["_ollama_schema_error"] = str(exc)
            return data


_semaphore_lock = threading.Lock()
_semaphore: Optional[threading.BoundedSemaphore] = None
_breakers: Dict[tuple, "_CircuitBreaker"] = {}
_breakers_lock = threading.Lock()


def _ollama_semaphore() -> threading.BoundedSemaphore:
    """Module-level semaphore shared by all clients so concurrent requests
    cannot pile up local model calls and exhaust machine resources."""
    global _semaphore
    with _semaphore_lock:
        if _semaphore is None:
            limit = max(1, int(os.environ.get("OLLAMA_MAX_CONCURRENT_CALLS", DEFAULT_MAX_CONCURRENT_CALLS)))
            _semaphore = threading.BoundedSemaphore(limit)
        return _semaphore


def _circuit_breaker(client: "OllamaClient") -> "_CircuitBreaker":
    # Breaker keyed per (base_url, model) so different configs are isolated.
    key = (client.base_url, client.model)
    with _breakers_lock:
        breaker = _breakers.get(key)
        if breaker is None:
            breaker = _CircuitBreaker(client.failure_threshold, client.cooldown_seconds)
            _breakers[key] = breaker
        return breaker
