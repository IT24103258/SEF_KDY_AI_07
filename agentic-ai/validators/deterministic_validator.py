from pydantic import BaseModel, ValidationError
from typing import Type, Tuple, Dict, Any

class DeterministicValidator:
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
    def check_human_approval_required(agent_name: str, output_data: Dict[str, Any]) -> Tuple[bool, str]:
        """
        Business rule check enforcing human-in-the-loop approval.
        """
        if agent_name == "ClassificationAgent":
            if output_data.get("confidence_score", 1.0) < 0.70:
                return True, "Low AI classification confidence score (< 70%). Human review required."
        elif agent_name == "PriorityAgent":
            if output_data.get("priority_level") == "Critical" or output_data.get("hazard_flag") is True:
                return True, "Critical priority or hazard flag detected. Human manager sign-off required."
        
        return False, ""
