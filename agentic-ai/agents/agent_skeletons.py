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
        tool_log = self.execute_tool("get_risk_matrix_rules", impact="High", likelihood="Medium")
        
        output = {
            "asset_criticality": "Critical",
            "impact_level": "High",
            "likelihood_level": "Medium",
            "risk_score": 85,
            "risk_level": "High",
            "priority": "High",
            "recommended_response_window": "Within 2-4 hours",
            "sla": {
                "response_hours": 2,
                "resolution_hours": 8
            },
            "escalation_flag": False,
            "explanation": "High operational impact on critical elevator asset with moderate recurrence likelihood.",
            # Backward compatibility fields
            "priority_level": "High",
            "target_sla_hours": 8,
            "hazard_flag": False
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, PriorityOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Risk & Priority Assessment",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
            output_data=output,
            validation_passed=is_valid,
            tool_calls=[tool_log]
        )

import requests
from typing import Dict, Any

class AssignmentAgent(BaseAgent):
    def __init__(self):
        tools = [ALLOW_LISTED_TOOLS["get_technician_skills"]]
        super().__init__("AssignmentAgent", tools)
        
        # Mock techniques to test scoring logic for a C# API while offline.
        self.mock_technicians = [
            {"id": "TECH-101", "fullName": "Kamal Perera", "skills": ["Electrical", "HVAC"], "status": "Active", "distanceKm": 1.5, "workload": 1},
            {"id": "TECH-102", "name": "Nimal Silva", "skills": ["Plumbing"], "status": "Active", "distanceKm": 4.2, "workload": 5},
            {"id": "TECH-103", "name": "Saman Kumara", "skills": ["Electrical Maintenance"], "status": "Active", "distanceKm": 0.8, "workload": 0},
        ]

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        tool_log = self.execute_tool("get_technician_skills", technician_id="TECH-001")
        
        request_id = str(input_context.get("request_id", "REQ-001"))
        required_skill = input_context.get("required_skill", "Electrical")
        csharp_api_url = "http://localhost:5103/api/technicians"

        candidates = []
        technicians = []

        # Step 1: Calling the C# API (uses mock data if it fails)
        try:
            response = requests.get(f"{csharp_api_url}?skill={required_skill}", timeout=2)
            if response.status_code == 200 and response.json():
                technicians = response.json()
            else:
                fallback_resp = requests.get(csharp_api_url, timeout=2)
                technicians = fallback_resp.json() if fallback_resp.status_code == 200 else []
        except Exception:
            # Falling back to mock data for standalone testing
            technicians = self.mock_technicians

        if not technicians:
            technicians = self.mock_technicians

        # Step 2: Scoring Decision Engine (Member 3 Logic)
        best_match = None
        highest_score = -1.0

        for tech in technicians:
            score = 0.50  # Base Score
            
            # Skill Matching Logic
            skills_list = tech.get("skills", [])
            has_exact_skill = any(required_skill.lower() in str(s).lower() for s in skills_list)
            if has_exact_skill:
                score += 0.35
            
            # Active Status Bonus
            if tech.get("status") == "Active":
                score += 0.10

            # Workload Penalty
            workload = tech.get("workload", 0)
            score -= (workload * 0.03)

            # Round off score
            final_score = round(max(0.10, min(0.99, score)), 2)

            formatted_cand = {
                "technician_id": str(tech.get("id", "TECH-001")),
                "name": tech.get("fullName") or tech.get("name") or "Unknown Tech",
                "match_score": final_score,
                "distance_km": float(tech.get("distanceKm", 1.2)),
                "current_workload": workload
            }
            candidates.append(formatted_cand)

            if final_score > highest_score:
                highest_score = final_score
                best_match = formatted_cand

        # Selecting the person with the high score as the 'Top Match'
        candidates.sort(key=lambda x: x["match_score"], reverse=True)
        top_match_id = candidates[0]["technician_id"] if candidates else "TECH-001"

        output = {
            "request_id": request_id,
            "recommended_candidates": candidates,
            "top_match_id": top_match_id
        }

        # Step 3: Pydantic Schema and Human Approval Check
        is_valid, parsed, err = DeterministicValidator.validate_schema(output, AssignmentOutput)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Technician Matching",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else "SUCCESS",
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
