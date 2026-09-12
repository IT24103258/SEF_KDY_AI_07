from agents.base_agent import BaseAgent
from tools.domain_tools import ALLOW_LISTED_TOOLS
from schemas.agent_schemas import ClassificationOutput, PriorityOutput, AssignmentOutput, ScheduleProposal
from schemas.workflow_schemas import StepExecutionResult
from validators.deterministic_validator import DeterministicValidator
from typing import Dict, Any

class ClassificationAgent(BaseAgent):
    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["get_asset_details"],
            ALLOW_LISTED_TOOLS["get_location_details"],
            ALLOW_LISTED_TOOLS["get_issue_category_rules"]
        ]
        super().__init__("ClassificationAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_issue_category_rules", category_name=input_context.get("title", ""))
        
        output = {
            "category": "Electrical Breakdown",
            "subcategory": "HVAC Chiller Power Supply",
            "confidence_score": 0.92,
            "requires_review": False
        }
        
        is_valid, parsed, err = DeterministicValidator.validate_schema(output, ClassificationOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Intake & Classification",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )

class PriorityAgent(BaseAgent):
    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["get_asset_details"],
            ALLOW_LISTED_TOOLS["get_sla_config"]
        ]
        super().__init__("PriorityAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_sla_config", priority_level="High")
        
        output = {
            "risk_score": 85,
            "priority_level": "High",
            "target_sla_hours": 8,
            "hazard_flag": False
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, PriorityOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Risk & Priority Scoring",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )

class AssignmentAgent(BaseAgent):
    def __init__(self):
        tools = [ALLOW_LISTED_TOOLS["get_technician_skills"]]
        super().__init__("AssignmentAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_technician_skills", technician_id="TECH-001")
        
        output = {
            "request_id": input_context.get("request_id", "REQ-001"),
            "recommended_candidates": [
                {
                    "technician_id": "TECH-001",
                    "name": "Senior Technician",
                    "match_score": 0.95,
                    "distance_km": 1.2,
                    "current_workload": 2
                }
            ],
            "top_match_id": "TECH-001"
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, AssignmentOutput)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Technician Matching",
            status="SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )

class SchedulingAgent(BaseAgent):
    def __init__(self):
        tools = [ALLOW_LISTED_TOOLS["get_location_details"]]
        super().__init__("SchedulingAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        output = {
            "request_id": input_context.get("request_id", "REQ-001"),
            "assigned_technician_id": "TECH-001",
            "proposed_start_time": "2026-09-15T09:00:00Z",
            "proposed_end_time": "2026-09-15T11:00:00Z",
            "is_conflict_free": True
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, ScheduleProposal)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Schedule Proposal",
            status="SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[]
        )
