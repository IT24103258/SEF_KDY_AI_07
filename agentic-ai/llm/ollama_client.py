import json
import logging
import os
import time
from typing import Any, Dict, Optional

import requests

logger = logging.getLogger(__name__)

DEFAULT_BASE_URL = "http://localhost:11434"
DEFAULT_MODEL = "llama3.2:latest"
DEFAULT_TIMEOUT_SECONDS = 30


class OllamaClientError(Exception):
    pass


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

    def generate_structured(
        self,
        system_prompt: str,
        user_prompt: str,
        expected_schema: Optional[type] = None,
    ) -> Dict[str, Any]:
        try:
            raw_text, latency_ms = self._call_generate(system_prompt, user_prompt)
        except OllamaClientError as exc:
            logger.warning("Ollama call failed: %s", exc)
            return {"_ollama_error": str(exc), "_ollama_latency_ms": 0}

        parsed = self._extract_json(raw_text)
        parsed["_ollama_latency_ms"] = latency_ms

        if expected_schema is not None:
            parsed = self._validate_with_schema(parsed, expected_schema)

        return parsed

    def is_available(self) -> bool:
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
        internal_keys = {"_ollama_error", "_ollama_latency_ms", "_ollama_parse_error", "_ollama_raw_preview"}
        if any(k in data for k in internal_keys):
            return data

        try:
            validated = schema_cls(**data)
            return validated.model_dump()
        except Exception as exc:
            logger.warning("Ollama output failed schema validation: %s", exc)
            data["_ollama_schema_error"] = str(exc)
            return data
