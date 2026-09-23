import re
from pydantic import BaseModel, ValidationError
from typing import Type, Tuple, Dict, Any, Optional, List

class DeterministicValidator:
    VALID_RISK_LEVELS = {"Low", "Medium", "High", "Critical"}
    VALID_PRIORITY_LEVELS = {"Low", "Medium", "High", "Critical"}

    INJECTION_PATTERNS = [
        r"ignore\s+(all\s+)?(previous|above|system)\s+(instructions|rules|prompts)",
        r"disregard\s+(the\s+)?(risk|safety|priority)\s+(rules|guidelines|calculations)",
        r"mark\s+(this\s+)?(as\s+)?(low|minimal|zero|safe)",
        r"set\s+(the\s+)?(risk|priority)\s+(to\s+)?(low|zero)",
        r"system\s+override",
        r"jailbreak",
        r"you\s+are\s+now\s+(in\s+)?(maintenance\s+)?debug\s+mode",
        r"do\s+not\s+(escalate|flag|alert)",
        r"downgrade\s+(this\s+)?(priority|risk)"
    ]

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
        Strips suspected prompt injection patterns from user text while preserving legitimate context.
        """
        if not text:
            return ""

        sanitized = text
        for pattern in DeterministicValidator.INJECTION_PATTERNS:
            sanitized = re.sub(pattern, "[FILTERED_INJECTION_ATTEMPT]", sanitized, flags=re.IGNORECASE)

        return sanitized.strip()

    @staticmethod
    def validate_schema(data: Dict[str, Any], schema_cls: Type[BaseModel]) -> Tuple[bool, Any, str]:
        """
        Validates agent dictionary output deterministically against a Pydantic schema class.
        Returns: (is_valid, parsed_model_or_none, error_message)
        """
        try:
            validated_instance = schema_cls(**data)
            return True, validated_instance, "Validation successful"
        except ValidationError as ve:
            return False, None, f"Schema validation error: {str(ve)}"
        except Exception as e:
            return False, None, f"Validation exception: {str(e)}"

    @staticmethod
    def validate_priority_assessment_rules(output_data: Dict[str, Any], input_context: Optional[Dict[str, Any]] = None) -> Tuple[bool, Dict[str, Any], str]:
        """
        Deterministic safety and business rule validator for Component 2 (Risk & Priority Assessment).
        Enforces:
        - Structural risk field checks
        - Valid risk_level & priority values (Low, Medium, High, Critical)
        - Risk score range (1-100)
        - Escalation flag boolean check
        - Deterministic safety override: Safety hazard / critical impact cannot be downgraded to Low/Medium
        """
        context = input_context or {}
        risk_level = output_data.get("risk_level") or output_data.get("priority_level")
        priority = output_data.get("priority") or output_data.get("priority_level")
        risk_score = output_data.get("risk_score")

        if risk_score is None or not (1 <= risk_score <= 100):
            return False, output_data, "Invalid risk_score: must be an integer between 1 and 100."

        if risk_level and risk_level not in DeterministicValidator.VALID_RISK_LEVELS:
            return False, output_data, f"Invalid risk_level: '{risk_level}'. Must be one of {DeterministicValidator.VALID_RISK_LEVELS}."

        if priority and priority not in DeterministicValidator.VALID_PRIORITY_LEVELS:
            return False, output_data, f"Invalid priority: '{priority}'. Must be one of {DeterministicValidator.VALID_PRIORITY_LEVELS}."

        # Deterministic Safety Override:
        # If input context indicates life safety / severe hazard / critical impact, ensure risk/priority is at least High
        has_hazard = (
            context.get("has_safety_hazard") is True or
            context.get("hazard_flag") is True or
            output_data.get("hazard_flag") is True or
            (output_data.get("asset_criticality") == "Critical" and output_data.get("impact_level") in ["High", "Critical"])
        )

        if has_hazard and (risk_level in ["Low", "Medium"] or priority in ["Low", "Medium"]):
            output_data["risk_level"] = "High"
            output_data["priority"] = "High"
            output_data["escalation_flag"] = True
            output_data["explanation"] = (output_data.get("explanation", "") + " [Deterministic Safety Rule: Elevated to High due to safety/critical impact]").strip()

        return True, output_data, "Deterministic priority validation passed."

    @staticmethod
    def check_human_approval_required(agent_name: str, output_data: Dict[str, Any]) -> Tuple[bool, str]:
        """
        Business rule check enforcing human-in-the-loop approval.
        """
        if agent_name == "ClassificationAgent":
            if output_data.get("confidence_score", 1.0) < 0.70:
                return True, "Low AI classification confidence score (< 70%). Human review required."
        elif agent_name == "PriorityAgent":
            risk_level = output_data.get("risk_level")
            priority = output_data.get("priority") or output_data.get("priority_level")
            is_critical = risk_level == "Critical" or priority == "Critical"
            has_escalation = output_data.get("escalation_flag") is True or output_data.get("hazard_flag") is True
            if is_critical or has_escalation:
                return True, "Critical risk/priority or escalation flag detected. Human manager sign-off required."
        
        return False, ""
