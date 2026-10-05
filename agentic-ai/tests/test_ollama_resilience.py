"""Ollama resilience tests — the LLM is optional and must never break the workflow.

Covers: disabled-by-config fail-fast, deterministic fallback provenance,
bounded retries (transport errors only), circuit breaker fail-fast and
half-open recovery, and the concurrency semaphore guard.
"""
import os
from unittest.mock import patch

import pytest

from agents.agent_skeletons import SchedulingAgent
from llm.ollama_client import (
    DEFAULT_MAX_ATTEMPTS,
    MAX_ALLOWED_ATTEMPTS,
    OllamaClient,
    OllamaClientError,
    _CircuitBreaker,
    _ollama_semaphore,
)


def _base_agent_input(request_id, description="Please schedule it in the afternoon"):
    return {
        "request_id": request_id,
        "assigned_technician_id": "TECH-001",
        "estimated_duration_minutes": 120,
        "preferred_start_time": "2026-09-24T14:00:00Z",
        "preferred_end_time": "2026-09-24T16:00:00Z",
        "sla_deadline": "2026-09-24T18:00:00Z",
        "description": description,
        "existing_bookings": [],
    }


def _unavailable_intent(marker_key):
    intent = {
        "_ollama_error": "Ollama unavailable in test.",
        "_ollama_latency_ms": 0,
    }
    intent[marker_key] = True
    return intent


def _successful_intent():
    return {
        "preference_type": "afternoon",
        "preferred_start": "14:00",
        "preferred_end": "17:00",
        "preferred_period": "afternoon",
        "urgency": "normal",
        "avoid_periods": [],
        "flexibility": "flexible",
        "requested_date": "2026-09-24",
        "requested_duration_minutes": 120,
        "interpretation_summary": "Requester prefers an afternoon slot.",
        "confidence": 0.92,
        "_ollama_latency_ms": 45,
    }


# ============================================================
# 1. Disabled by configuration — no model call, no network
# ============================================================

class TestOllamaDisabledByConfig:

    def test_disabled_client_fails_fast_without_model_call(self):
        with patch.dict(os.environ, {"OLLAMA_ENABLED": "false"}):
            client = OllamaClient(base_url="http://disabled-test.invalid")
            assert client.enabled is False
            with patch.object(client, "_call_generate") as mock_call:
                result = client.generate_structured("system", "user")
                assert result["_ollama_disabled"] is True
                assert "_ollama_error" in result
                assert result["_ollama_latency_ms"] == 0
                mock_call.assert_not_called()

    def test_disabled_client_is_available_reports_false_without_network(self):
        with patch.dict(os.environ, {"OLLAMA_ENABLED": "false"}):
            client = OllamaClient(base_url="http://disabled-test.invalid")
            with patch("llm.ollama_client.requests.get") as mock_get:
                assert client.is_available() is False
                mock_get.assert_not_called()


# ============================================================
# 2. Agent provenance — fallback is explicit, never disguised as LLM output
# ============================================================

class TestAgentDeterministicFallbackProvenance:

    @pytest.mark.parametrize("marker_key", [
        "_ollama_disabled",
        "_ollama_circuit_open",
        "_ollama_busy",
    ])
    @patch("agents.agent_skeletons._interpret_scheduling_intent")
    def test_unavailable_llm_produces_deterministic_fallback(self, mock_interpret, marker_key):
        mock_interpret.return_value = _unavailable_intent(marker_key)
        agent = SchedulingAgent()
        res = agent.run_step(_base_agent_input("REQ-RES-001"))

        assert res.output_data["llm_used"] is False
        assert res.output_data["llm_fallback"] is True
        assert res.output_data["scheduling_path"] == "deterministic_fallback"
        # The workflow still completes deterministically and stays pending manager approval.
        assert res.status == "REQUIRES_HUMAN_APPROVAL"
        assert res.output_data["proposal_status"] == "Proposed"
        assert res.output_data["validation_required"] is True
        assert res.validation_passed is True
        assert res.output_data["is_conflict_free"] is True
        assert len(res.tool_calls) == 5

    @patch("agents.agent_skeletons._interpret_scheduling_intent")
    def test_disabled_summary_is_recorded_for_observability(self, mock_interpret):
        mock_interpret.return_value = _unavailable_intent("_ollama_disabled")
        agent = SchedulingAgent()
        res = agent.run_step(_base_agent_input("REQ-RES-002"))

        assert "disabled" in res.output_data["llm_interpretation_summary"].lower()
        assert res.output_data["llm_confidence"] != 0.92  # real LLM confidence not used

    @patch("agents.agent_skeletons._interpret_scheduling_intent")
    def test_llm_assisted_path_is_labelled_llm_assisted(self, mock_interpret):
        mock_interpret.return_value = _successful_intent()
        agent = SchedulingAgent()
        res = agent.run_step(_base_agent_input("REQ-RES-003"))

        assert res.output_data["llm_used"] is True
        assert res.output_data["llm_fallback"] is False
        assert res.output_data["scheduling_path"] == "llm_assisted"
        # Even on the LLM path the proposal stays pending manager approval.
        assert res.status == "REQUIRES_HUMAN_APPROVAL"
        assert res.output_data["validation_required"] is True


# ============================================================
# 3. Bounded retries — transport errors only, never parse errors
# ============================================================

class TestBoundedRetries:

    def test_transport_error_retried_then_succeeds(self):
        client = OllamaClient(base_url="http://retry-ok.invalid")
        client.max_attempts = 2
        with patch.object(
            client,
            "_call_generate",
            side_effect=[OllamaClientError("connection refused"), ('{"confidence": 0.9}', 5)],
        ) as mock_call:
            result = client.generate_structured("system", "user")
            assert result["confidence"] == 0.9
            assert result["_ollama_latency_ms"] == 5
            assert mock_call.call_count == 2

    def test_all_attempts_exhausted_returns_error_marker_not_exception(self):
        client = OllamaClient(base_url="http://retry-exhaust.invalid")
        client.max_attempts = 2
        with patch.object(
            client,
            "_call_generate",
            side_effect=OllamaClientError("Ollama is not running"),
        ) as mock_call:
            result = client.generate_structured("system", "user")
            assert result["_ollama_error"] == "Ollama is not running"
            assert mock_call.call_count == 2

    def test_parse_errors_are_not_retried(self):
        client = OllamaClient(base_url="http://retry-parse.invalid")
        client.max_attempts = 3
        with patch.object(
            client,
            "_call_generate",
            return_value=("this is not json at all", 7),
        ) as mock_call:
            result = client.generate_structured("system", "user")
            assert result["_ollama_parse_error"] is True
            assert mock_call.call_count == 1  # deterministic failure, no retry burn

    def test_max_attempts_capped_at_allowed_limit(self):
        with patch.dict(os.environ, {"OLLAMA_MAX_ATTEMPTS": "99"}):
            client = OllamaClient()
            assert client.max_attempts == MAX_ALLOWED_ATTEMPTS
            assert client.max_attempts <= 3

    def test_default_is_single_attempt(self):
        with patch.dict(os.environ, {"OLLAMA_ENABLED": "false", "OLLAMA_MAX_ATTEMPTS": "1"}, clear=True):
            client = OllamaClient()
            assert client.max_attempts == DEFAULT_MAX_ATTEMPTS


# ============================================================
# 4. Circuit breaker — fail fast, per-endpoint isolation, half-open
# ============================================================

class TestCircuitBreaker:

    def test_breaker_opens_after_consecutive_failures_and_fails_fast(self):
        client = OllamaClient(base_url="http://cb-open.invalid")
        client.failure_threshold = 2
        client.cooldown_seconds = 60
        with patch.object(
            client,
            "_call_generate",
            side_effect=OllamaClientError("model overloaded"),
        ) as mock_call:
            first = client.generate_structured("system", "user")
            second = client.generate_structured("system", "user")
            third = client.generate_structured("system", "user")

            assert "_ollama_error" in first
            assert first.get("_ollama_circuit_open") is None
            assert "_ollama_error" in second
            assert third["_ollama_circuit_open"] is True
            assert mock_call.call_count == 2  # third call never reached the model

    def test_breaker_record_success_resets_failures(self):
        breaker = _CircuitBreaker(threshold=2, cooldown_seconds=60)
        breaker.record_failure()
        breaker.record_failure()
        opened, _ = breaker.is_open()
        assert opened is True

        breaker.record_success()
        opened, _ = breaker.is_open()
        assert opened is False

    def test_breaker_half_open_after_cooldown_expires(self):
        breaker = _CircuitBreaker(threshold=1, cooldown_seconds=0)
        breaker.record_failure()
        opened, _ = breaker.is_open()
        # Cooldown already elapsed: one probe attempt is allowed through.
        assert opened is False

    def test_breakers_isolated_per_endpoint(self):
        failing = OllamaClient(base_url="http://cb-a.invalid")
        failing.failure_threshold = 1
        failing.cooldown_seconds = 60
        with patch.object(failing, "_call_generate", side_effect=OllamaClientError("down")):
            failing.generate_structured("system", "user")
            second = failing.generate_structured("system", "user")
            assert second["_ollama_circuit_open"] is True

        healthy = OllamaClient(base_url="http://cb-b.invalid")
        healthy.failure_threshold = 1
        with patch.object(healthy, "_call_generate", return_value=('{"ok": 1}', 1)):
            result = healthy.generate_structured("system", "user")
            assert "_ollama_error" not in result  # other endpoint unaffected


# ============================================================
# 5. Concurrency guard — one model call at a time, always released
# ============================================================

class TestConcurrencyLimit:

    def test_semaphore_exhaustion_fails_fast_without_model_call(self):
        semaphore = _ollama_semaphore()
        assert semaphore.acquire(timeout=1) is True
        try:
            client = OllamaClient(base_url="http://busy-test.invalid")
            with patch.object(client, "_call_generate") as mock_call:
                result = client.generate_structured("system", "user")
                assert result["_ollama_busy"] is True
                assert "_ollama_error" in result
                mock_call.assert_not_called()
        finally:
            semaphore.release()

    def test_semaphore_released_after_successful_call(self):
        client = OllamaClient(base_url="http://semaphore-release.invalid")
        with patch.object(client, "_call_generate", return_value=('{"ok": 1}', 1)):
            client.generate_structured("system", "user")

        # A leaked semaphore would make this acquire time out and break every
        # subsequent Ollama call in the process.
        semaphore = _ollama_semaphore()
        assert semaphore.acquire(timeout=1) is True
        semaphore.release()
