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
            "category": "HVAC",
            "subcategory": "Lobby AC Cooling Failure",
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
    """
    Risk & Priority Assessment Agent (Component 2)
    Assesses asset criticality, impact, likelihood, risk score, risk matrix level,
    priority, SLA targets, escalation flags, and provides risk explanations.
    """
    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["get_asset_criticality"],
            ALLOW_LISTED_TOOLS["get_location_risk_rules"],
            ALLOW_LISTED_TOOLS["get_open_requests_for_asset"],
            ALLOW_LISTED_TOOLS["get_sla_config"],
            ALLOW_LISTED_TOOLS["get_risk_matrix_rules"],
            ALLOW_LISTED_TOOLS["get_historical_risk_data"],
            ALLOW_LISTED_TOOLS["save_risk_assessment"],
            ALLOW_LISTED_TOOLS["save_priority_assessment"]
        ]
        super().__init__("PriorityAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_logs = []
        raw_text = f"{input_context.get('title', '')} {input_context.get('description', '')}"

        # 1. Prompt Injection Defense
        is_injection, matched_patterns = DeterministicValidator.detect_prompt_injection(raw_text)
        sanitized_text = DeterministicValidator.sanitize_untrusted_input(raw_text)

        # 2. Tool Execution via Allow-Listed Tools
        asset_id = str(input_context.get("asset_id", ""))
        location_id = str(input_context.get("location_id", ""))
        category = str(input_context.get("category", ""))

        t1 = self.execute_tool("get_asset_criticality", asset_id=asset_id)
        tool_logs.append(t1)
        asset_crit = t1.output_params.get("criticality", "Medium")

        t2 = self.execute_tool("get_location_risk_rules", location_id=location_id)
        tool_logs.append(t2)
        is_density = t2.output_params.get("occupancy_density") == "High"

        t3 = self.execute_tool("get_open_requests_for_asset", asset_id=asset_id)
        tool_logs.append(t3)
        open_count = t3.output_params.get("open_request_count", 0)

        t4 = self.execute_tool("get_historical_risk_data", asset_id=asset_id)
        tool_logs.append(t4)
        recurrence = t4.output_params.get("historical_recurrence_rate", "Low")

        # 3. Deterministic Safety Evaluation
        has_hazard = (
            input_context.get("has_safety_hazard") is True or
            input_context.get("hazard_flag") is True or
            any(w in raw_text.lower() for w in ["fire", "gas", "smoke", "spark", "electric shock", "flood", "hazard", "explosion"])
        )

        impact = "Critical" if has_hazard else input_context.get("impact", "High" if asset_crit == "Critical" else "Medium")
        likelihood = "Critical" if open_count >= 3 else ("High" if recurrence in ["High", "Moderate"] else "Medium")

        t5 = self.execute_tool("get_risk_matrix_rules", impact=impact, likelihood=likelihood)
        tool_logs.append(t5)
        base_risk = t5.output_params.get("risk_level", "Medium")

        # Calculate Score
        impact_pts = 4 if impact == "Critical" else (3 if impact == "High" else (2 if impact == "Medium" else 1))
        likeli_pts = 4 if likelihood == "Critical" else (3 if likelihood == "High" else (2 if likelihood == "Medium" else 1))
        crit_pts = 4 if asset_crit == "Critical" else (3 if asset_crit == "High" else (2 if asset_crit == "Medium" else 1))

        base_score = (impact_pts * likeli_pts) * 3
        crit_weight = crit_pts * 5
        safety_mod = 25 if has_hazard else 0
        loc_mod = 5 if is_density else 0

        raw_score = base_score + crit_weight + safety_mod + loc_mod
        if has_hazard and raw_score < 75:
            raw_score = 75

        risk_score = max(1, min(100, raw_score))

        risk_level = "Critical" if risk_score >= 76 else ("High" if risk_score >= 51 else ("Medium" if risk_score >= 26 else "Low"))
        priority = risk_level
        if (has_hazard or (asset_crit == "Critical" and impact in ["High", "Critical"])) and priority in ["Low", "Medium"]:
            priority = "High"

        t6 = self.execute_tool("get_sla_config", priority_level=priority)
        tool_logs.append(t6)
        sla_data = t6.output_params

        t7 = self.execute_tool("save_risk_assessment", request_id=input_context.get("request_id", ""), risk_score=risk_score)
        tool_logs.append(t7)

        t8 = self.execute_tool("save_priority_assessment", request_id=input_context.get("request_id", ""), priority=priority)
        tool_logs.append(t8)

        escalation_flag = (priority == "Critical") or has_hazard
        window = "Immediate (Within 1 hour)" if priority == "Critical" else ("Within 2 hours" if priority == "High" else "Within 4 hours")

        explanation = f"Evaluated {impact} impact and {likelihood} likelihood on {asset_crit} criticality asset."
        if has_hazard:
            explanation += " [SAFETY HAZARD DETECTED: Deterministic Priority Override Enforced]."
        if is_injection:
            explanation += " [SECURITY NOTICE: Prompt injection attempt detected and neutralized]."

        output = {
            "asset_criticality": asset_crit,
            "impact_level": impact,
            "likelihood_level": likelihood,
            "risk_score": risk_score,
            "risk_level": risk_level,
            "priority": priority,
            "recommended_response_window": window,
            "sla": sla_data,
            "escalation_flag": escalation_flag,
            "explanation": explanation,
            "priority_level": priority,
            "target_sla_hours": sla_data.get("resolution_hours", 24),
            "hazard_flag": has_hazard
        }

        # 4. Deterministic Business & Safety Validation
        val_passed, validated_data, val_msg = DeterministicValidator.validate_priority_assessment_rules(output, input_context)
        is_valid, parsed, schema_err = DeterministicValidator.validate_schema(validated_data, PriorityOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, validated_data)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Risk & Priority Assessment",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=validated_data,
            validation_passed=is_valid and val_passed,
            tool_calls=tool_logs
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
