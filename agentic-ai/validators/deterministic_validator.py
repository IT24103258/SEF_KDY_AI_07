from pydantic import BaseModel, ValidationError
from typing import Type, Tuple, Dict, Any, Optional, List
from datetime import datetime

def _parse_iso_timestamp(ts: Optional[str]) -> Optional[datetime]:
    if not ts or not isinstance(ts, str):
        return None
    try:
        clean_ts = ts.replace("Z", "+00:00")
        return datetime.fromisoformat(clean_ts)
    except Exception:
        return None

class DeterministicValidator:
    VALID_RISK_LEVELS = {"Low", "Medium", "High", "Critical"}
    VALID_PRIORITY_LEVELS = {"Low", "Medium", "High", "Critical"}

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
    def validate_schedule_proposal(output_data: Dict[str, Any], input_context: Optional[Dict[str, Any]] = None) -> Tuple[bool, Dict[str, Any], str]:
        """
        Deterministic safety and business rule validator for Component 4 (Scheduling & Work Order Management).
        Enforces:
        - Start time strictly earlier than end time
        - Positive estimated duration
        - Business hours compliance
        - Technician availability compliance
        - Independent overlap conflict detection against existing bookings: (exist_start < prop_end) and (exist_end > prop_start)
        - SLA deadline compliance verification
        """
        context = input_context or {}
        duration = output_data.get("estimated_duration_minutes", 0)
        if duration <= 0:
            return False, output_data, "Invalid duration: must be positive minutes."

        start_str = output_data.get("proposed_start") or output_data.get("proposed_start_time")
        end_str = output_data.get("proposed_end") or output_data.get("proposed_end_time")

        if not start_str or not end_str:
            return False, output_data, "Missing proposed start or end timestamp."

        dt_start = _parse_iso_timestamp(start_str)
        dt_end = _parse_iso_timestamp(end_str)

        if dt_start and dt_end:
            if dt_start >= dt_end:
                return False, output_data, "Proposed start time must be strictly earlier than proposed end time."
        else:
            if start_str >= end_str:
                return False, output_data, "Proposed start time must be strictly earlier than proposed end time."

        if output_data.get("within_business_hours") is False:
            return False, output_data, "Schedule rejected: Proposed slot falls outside operational business hours."

        if output_data.get("within_technician_availability") is False:
            return False, output_data, "Schedule rejected: Technician is not available for the requested slot."

        # Deterministic Overlap Conflict Evaluation against existing bookings
        bookings = output_data.get("existing_bookings") or context.get("existing_bookings") or []
        for b in bookings:
            b_start_str = b.get("start_time") or b.get("start") or b.get("proposed_start")
            b_end_str = b.get("end_time") or b.get("end") or b.get("proposed_end")
            b_start = _parse_iso_timestamp(b_start_str)
            b_end = _parse_iso_timestamp(b_end_str)
            if b_start and b_end and dt_start and dt_end:
                if b_start < dt_end and b_end > dt_start:
                    output_data["conflict_detected"] = True
                    output_data["is_conflict_free"] = False
                    return False, output_data, f"Schedule conflict detected with existing work order ({b_start_str} - {b_end_str})."
            elif b_start_str and b_end_str:
                if b_start_str < end_str and b_end_str > start_str:
                    output_data["conflict_detected"] = True
                    output_data["is_conflict_free"] = False
                    return False, output_data, f"Schedule conflict detected with existing work order ({b_start_str} - {b_end_str})."

        if output_data.get("conflict_detected") is True:
            output_data["is_conflict_free"] = False
            return False, output_data, "Schedule conflict detected with existing work order."

        # SLA Deadline Verification
        sla_str = output_data.get("sla_deadline") or context.get("sla_deadline")
        if sla_str:
            dt_sla = _parse_iso_timestamp(sla_str)
            if dt_sla and dt_end:
                if dt_end > dt_sla:
                    output_data["sla_compliant"] = False
                    return False, output_data, f"Proposed schedule breaches SLA deadline ({sla_str})."
                else:
                    output_data["sla_compliant"] = True
            elif end_str and end_str > sla_str:
                output_data["sla_compliant"] = False
                return False, output_data, f"Proposed schedule breaches SLA deadline ({sla_str})."
            else:
                output_data["sla_compliant"] = True

        if output_data.get("sla_compliant") is False:
            return False, output_data, "Schedule rejected: Proposed slot breaches SLA deadline."

        output_data["is_conflict_free"] = True
        return True, output_data, "Deterministic schedule validation passed."

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
        elif agent_name == "SchedulingAgent":
            # Mandatory manager approval gate for all work-order schedule proposals
            return True, "Work order schedule proposal requires Manager sign-off before dispatch."
        
        return False, ""

