"""
Component 2 — Deterministic Validator.

Authoritative thresholds (must match Python agent and C# service):
  score 1..25   → Low
  score 26..50  → Medium
  score 51..75  → High
  score 76..100 → Critical

These thresholds are enforced in TWO places:
  - Python agent  (agent_skeletons.PriorityAgent._score_to_risk_level)
  - This validator (DeterministicValidator.validate_priority_assessment_rules)
"""

import re
from pydantic import BaseModel, ValidationError
from typing import Type, Tuple, Dict, Any, Optional, List

class DeterministicValidator:
    VALID_RISK_LEVELS     = {"Low", "Medium", "High", "Critical"}
    VALID_PRIORITY_LEVELS = {"Low", "Medium", "High", "Critical"}

    # Authoritative score→risk-level thresholds
    RISK_SCORE_BANDS = [
        (76, 100, "Critical"),
        (51, 75,  "High"),
        (26, 50,  "Medium"),
        (1,  25,  "Low"),
    ]

    # -----------------------------------------------------------------------
    # Prompt-injection patterns — covers a broad set of adversarial inputs
    # -----------------------------------------------------------------------
    INJECTION_PATTERNS = [
        # Classic "ignore previous" variants
        r"ignore\s+(all\s+)?(previous|above|system)\s+(instructions|rules|prompts)",
        # Ignore any policy/rules (covers "Ignore the safety policy")
        r"ignore\s+(the\s+)?(safety|risk|hazard|priority|security)\s+(policy|rules|guidelines|flag|information)",
        # Disregard safety/risk rules
        r"disregard\s+(the\s+)?(risk|safety|priority|hazard)\s+(rules|guidelines|calculations|information|policy)",
        # Marking as low/safe
        r"mark\s+(this\s+)?(as\s+)?(low|minimal|zero|safe)",
        r"set\s+(the\s+)?(risk|priority)\s+(to\s+)?(low|zero)",
        # System overrides and jailbreaks
        r"system\s+override",
        r"jailbreak",
        r"you\s+are\s+now\s+(in\s+)?(maintenance\s+)?debug\s+mode",
        # Suppressing escalation / safety flags
        r"do\s+not\s+(escalate|flag|alert|report)",
        r"downgrade\s+(this\s+)?(priority|risk)",
        # Direct classification override attempts
        r"classify\s+(this\s+)?(incident\s+)?(as\s+)?(low|safe|minimal)",
        r"approve\s+(this\s+)?(request\s+)?(as\s+)?(low|safe)",
        # Hazard forgetting
        r"forget\s+(the\s+)?(hazard|safety|risk)\s+(information|data|flag)?",
        # Instruction injection via "system:" prefix
        r"system\s*:\s*(override|set|mark|classify|ignore)",
    ]

    # -----------------------------------------------------------------------
    # Prompt-injection detection
    # -----------------------------------------------------------------------

    @staticmethod
    def detect_prompt_injection(text: str) -> Tuple[bool, List[str]]:
        """
        Scans untrusted input text for prompt injection attempts.
        Returns: (is_injection_detected, matched_patterns)
        """
        if not text:
            return False, []

        matches = []
        for pattern in DeterministicValidator.INJECTION_PATTERNS:
            if re.search(pattern, text, re.IGNORECASE):
                matches.append(pattern)

        return len(matches) > 0, matches

    @staticmethod
    def sanitize_untrusted_input(text: str) -> str:
        """
        Strips suspected prompt injection patterns from user text while
        preserving legitimate context.  The returned value is the ONLY
        version of user-supplied text that may be passed to an LLM or
        embedded in prompts.
        """
        if not text:
            return ""

        sanitized = text
        for pattern in DeterministicValidator.INJECTION_PATTERNS:
            sanitized = re.sub(
                pattern,
                "[FILTERED_INJECTION_ATTEMPT]",
                sanitized,
                flags=re.IGNORECASE
            )

        return sanitized.strip()

    # -----------------------------------------------------------------------
    # Schema validation
    # -----------------------------------------------------------------------

    @staticmethod
    def validate_schema(data: Dict[str, Any], schema_cls: Type[BaseModel]) -> Tuple[bool, Any, str]:
        """
        Validates agent output against a Pydantic schema class.
        Returns: (is_valid, parsed_model_or_none, error_message)
        """
        try:
            validated_instance = schema_cls(**data)
            return True, validated_instance, "Validation successful"
        except ValidationError as ve:
            return False, None, f"Schema validation error: {str(ve)}"
        except Exception as e:
            return False, None, f"Validation exception: {str(e)}"

    # -----------------------------------------------------------------------
    # Score ↔ risk-level consistency check
    # -----------------------------------------------------------------------

    @staticmethod
    def score_matches_risk_level(risk_score: int, risk_level: str) -> bool:
        """
        Returns True only if the numeric score falls within the authoritative
        band for the given risk_level.

        score 1..25   → Low
        score 26..50  → Medium
        score 51..75  → High
        score 76..100 → Critical
        """
        for lo, hi, level in DeterministicValidator.RISK_SCORE_BANDS:
            if lo <= risk_score <= hi:
                return risk_level == level
        return False

    @staticmethod
    def expected_risk_level(risk_score: int) -> Optional[str]:
        """Returns the expected risk level for a given score, or None if out of range."""
        for lo, hi, level in DeterministicValidator.RISK_SCORE_BANDS:
            if lo <= risk_score <= hi:
                return level
        return None

    # -----------------------------------------------------------------------
    # Business-rule validator
    # -----------------------------------------------------------------------

    @staticmethod
    def validate_priority_assessment_rules(
        output_data: Dict[str, Any],
        input_context: Optional[Dict[str, Any]] = None
    ) -> Tuple[bool, Dict[str, Any], str]:
        """
        Deterministic safety and business rule validator for Component 2
        (Risk & Priority Assessment).

        Enforces:
        1. risk_score in range 1..100
        2. risk_level is a valid level
        3. priority is a valid level
        4. Score/risk-level consistency (rejects contradictory combinations)
        5. Safety hazard override (hazard cannot result in Low or Medium)
        6. priority cannot be lower than risk_level when hazard is present
        """
        context = input_context or {}
        risk_level = output_data.get("risk_level") or output_data.get("priority_level")
        priority   = output_data.get("priority")   or output_data.get("priority_level")
        risk_score = output_data.get("risk_score")

        # --- 1. Score range ---
        if risk_score is None or not isinstance(risk_score, (int, float)) or not (1 <= int(risk_score) <= 100):
            return False, output_data, "Invalid risk_score: must be an integer between 1 and 100."
        risk_score = int(risk_score)

        # --- 2. risk_level enum ---
        if risk_level and risk_level not in DeterministicValidator.VALID_RISK_LEVELS:
            return False, output_data, (
                f"Invalid risk_level: '{risk_level}'. "
                f"Must be one of {DeterministicValidator.VALID_RISK_LEVELS}."
            )

        # --- 3. priority enum ---
        if priority and priority not in DeterministicValidator.VALID_PRIORITY_LEVELS:
            return False, output_data, (
                f"Invalid priority: '{priority}'. "
                f"Must be one of {DeterministicValidator.VALID_PRIORITY_LEVELS}."
            )

        # --- 4. Score / risk-level consistency ---
        if risk_level and risk_score is not None:
            if not DeterministicValidator.score_matches_risk_level(risk_score, risk_level):
                expected = DeterministicValidator.expected_risk_level(risk_score)
                return False, output_data, (
                    f"Score/risk-level mismatch: score={risk_score} implies "
                    f"'{expected}' but risk_level='{risk_level}'. "
                    "Contradictory assessment rejected."
                )

        # --- 5 & 6. Safety hazard override ---
        # Physical safety hazard: detected ONLY via explicit flags.
        # NOTE: Critical asset + High impact is NOT implicitly a physical hazard.
        # A Critical asset can have High impact without any physical safety hazard.
        # Physical hazard detection is the responsibility of Python step 6 and C# DetectHazard(),
        # which inspect actual text for gas leaks, smoke, fire, trapped persons, etc.
        has_hazard = (
            context.get("has_safety_hazard") is True
            or context.get("hazard_flag") is True
            or output_data.get("hazard_flag") is True
            or output_data.get("hazard_detected") is True
        )

        if has_hazard:
            # Deterministic safety rule for a genuine physical hazard.
            # The authoritative formula guarantees a hazard scores >= 75; enforce that
            # floor here too, then re-derive risk_level from the score so the assessment
            # stays internally consistent (never downgrade a Critical score to High).
            original_score = risk_score
            original_level = output_data.get("risk_level")
            original_priority = output_data.get("priority") or output_data.get("priority_level")

            if risk_score < 75:
                risk_score = 75
                output_data["risk_score"] = 75

            # risk_level MUST match the score band.
            correct_level = DeterministicValidator.expected_risk_level(risk_score) or "High"
            output_data["risk_level"] = correct_level

            # priority is at least High for a hazard, and never below the risk_level.
            rank = {"Low": 1, "Medium": 2, "High": 3, "Critical": 4}
            inv = {1: "Low", 2: "Medium", 3: "High", 4: "Critical"}
            cur_priority_rank = rank.get(original_priority, 1)
            target_rank = max(cur_priority_rank, rank.get(correct_level, 3), 3)
            output_data["priority"] = inv[target_rank]
            output_data["escalation_flag"] = True

            changed = (
                original_score != output_data["risk_score"]
                or original_level != output_data["risk_level"]
                or original_priority != output_data["priority"]
            )
            if changed:
                output_data["explanation"] = (
                    (output_data.get("explanation", "") +
                     " [Deterministic Safety Rule: hazard enforced minimum score 75 and "
                     "elevated risk_level/priority to stay consistent with the hazard floor.]")
                    .strip()
                )

        return True, output_data, "Deterministic priority validation passed."

    # -----------------------------------------------------------------------
    # Human-approval check
    # -----------------------------------------------------------------------

    @staticmethod
    def check_human_approval_required(
        agent_name: str,
        output_data: Dict[str, Any]
    ) -> Tuple[bool, str]:
        """
        Business rule check enforcing human-in-the-loop approval.
        """
        if agent_name == "ClassificationAgent":
            if output_data.get("confidence_score", 1.0) < 0.70:
                return True, "Low AI classification confidence score (< 70%). Human review required."

        elif agent_name == "PriorityAgent":
            risk_level  = output_data.get("risk_level")
            priority    = output_data.get("priority") or output_data.get("priority_level")
            is_critical = risk_level == "Critical" or priority == "Critical"
            has_escalation = (
                output_data.get("escalation_flag") is True
                or output_data.get("hazard_flag") is True
            )
            if is_critical or has_escalation:
                return True, (
                    "Critical risk/priority or escalation/safety-hazard flag detected. "
                    "Flagged for downstream human review (Component 4 owns the approval decision)."
                )

        return False, ""

