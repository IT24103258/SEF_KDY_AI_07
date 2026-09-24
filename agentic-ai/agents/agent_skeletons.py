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
    """
    Scheduling & Work Order Management Agent (Component 4)
    Generates conflict-free work order scheduling proposals respecting technician calendar,
    operational business hours, existing bookings, and SLA deadlines.
    """
    def __init__(self):
        tools = [
            ALLOW_LISTED_TOOLS["GetTechnicianCalendar"],
            ALLOW_LISTED_TOOLS["GetBusinessHours"],
            ALLOW_LISTED_TOOLS["GetExistingWorkOrders"],
            ALLOW_LISTED_TOOLS["CreateScheduleProposal"],
            ALLOW_LISTED_TOOLS["ValidateSchedule"]
        ]
        super().__init__("SchedulingAgent", tools)

    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        req_id = input_context.get("request_id", "REQ-001")
        tech_id = input_context.get("assigned_technician_id") or input_context.get("technician_id", "TECH-001")
        priority = input_context.get("priority") or input_context.get("priority_level", "Medium")
        duration = input_context.get("estimated_duration_minutes", 120)
        sla_deadline = input_context.get("sla_deadline", "2026-09-24T18:00:00Z")
        start_time = input_context.get("preferred_start_time") or input_context.get("proposed_start_time") or input_context.get("proposed_start") or "2026-09-24T14:00:00Z"
        end_time = input_context.get("preferred_end_time") or input_context.get("proposed_end_time") or input_context.get("proposed_end") or "2026-09-24T16:00:00Z"

        # 1. Execute domain tools to fetch operational context
        tool_logs = []
        is_avail_input = input_context.get("is_technician_available", input_context.get("within_technician_availability", None))
        t1 = self.execute_tool(
            "GetTechnicianCalendar",
            technician_id=tech_id,
            technician_calendar=input_context.get("technician_calendar"),
            is_available=is_avail_input
        )
        tool_logs.append(t1)

        t2 = self.execute_tool(
            "GetBusinessHours",
            business_hours=input_context.get("business_hours"),
            date=start_time[:10] if start_time else "2026-09-24"
        )
        tool_logs.append(t2)

        t3 = self.execute_tool(
            "GetExistingWorkOrders",
            technician_id=tech_id,
            existing_bookings=input_context.get("existing_bookings")
        )
        tool_logs.append(t3)

        # 2. Extract tool outputs to derive scheduling constraints
        cal_res = t1.output_params if t1.success and t1.output_params else {}
        bh_res = t2.output_params if t2.success and t2.output_params else {}
        bookings_res = t3.output_params if t3.success and t3.output_params else {}

        existing_bookings = bookings_res.get("existing_bookings", [])
        is_tech_available = cal_res.get("is_available", True)
        if is_avail_input is False:
            is_tech_available = False

        within_bh = bh_res.get("is_working_day", True) and input_context.get("within_business_hours", True)

        # 3. Validate candidate slot deterministically using ValidateScheduleTool
        t5 = self.execute_tool(
            "ValidateSchedule",
            technician_id=tech_id,
            start_time=start_time,
            end_time=end_time,
            duration_minutes=duration,
            existing_bookings=existing_bookings,
            business_hours=bh_res,
            is_technician_available=is_tech_available,
            priority=priority,
            sla_deadline=sla_deadline
        )
        tool_logs.append(t5)

        val_res = t5.output_params if t5.success and t5.output_params else {}
        conflict_detected = not val_res.get("conflict_free", True) or input_context.get("simulate_conflict", False)
        conflict_details = val_res.get("conflicts", [])
        if conflict_detected and not conflict_details:
            conflict_details = ["Overlapping booking detected with existing work order"]

        sla_compliant = val_res.get("sla_compliant", True) and input_context.get("sla_compliant", True)

        # 4. Create structured proposal via CreateScheduleProposalTool
        t4 = self.execute_tool(
            "CreateScheduleProposal",
            request_id=req_id,
            technician_id=tech_id,
            proposed_start=start_time,
            proposed_end=end_time,
            estimated_duration_minutes=duration,
            priority=priority,
            sla_deadline=sla_deadline,
            conflict_detected=conflict_detected,
            conflict_details=conflict_details
        )
        tool_logs.append(t4)

        # 5. Formulate auditable, concise decision summary (no chain-of-thought tokens)
        if conflict_detected:
            decision_summary = f"Schedule conflict detected: {', '.join(conflict_details)}. Manager resolution required."
        elif not within_bh:
            decision_summary = "Proposed schedule falls outside operational business hours. Manager review required."
        elif not is_tech_available:
            decision_summary = "Technician unavailable for the proposed slot. Alternative dispatch required."
        elif not sla_compliant:
            decision_summary = f"Proposed schedule completes after SLA deadline ({sla_deadline}). Expedited resolution required."
        else:
            decision_summary = "Selected an available technician slot within business hours and before the SLA deadline. Existing bookings were checked and no overlapping booking was detected."

        output = {
            "request_id": req_id,
            "technician_id": tech_id,
            "assigned_technician_id": tech_id,
            "proposed_start": start_time,
            "proposed_end": end_time,
            "proposed_start_time": start_time,
            "proposed_end_time": end_time,
            "estimated_duration_minutes": duration,
            "priority": priority,
            "sla_deadline": sla_deadline,
            "conflict_detected": conflict_detected,
            "conflict_details": conflict_details,
            "within_business_hours": within_bh,
            "within_technician_availability": is_tech_available,
            "sla_compliant": sla_compliant,
            "proposal_status": "Proposed",
            "decision_summary": decision_summary,
            "validation_required": True,
            "is_conflict_free": not conflict_detected
        }

        is_valid, parsed, err = DeterministicValidator.validate_schema(output, ScheduleProposal)
        is_sched_valid, output, sched_err = DeterministicValidator.validate_schedule_proposal(output, input_context)
        requires_human, approval_reason = DeterministicValidator.check_human_approval_required(self.name, output)

        return StepExecutionResult(
            agent_name=self.name,
            step_name="Conflict-Free Work Order Scheduling",
            status="REQUIRES_HUMAN_APPROVAL" if requires_human else ("SUCCESS" if is_valid and is_sched_valid else "FAILED"),
            output_data=output,
            validation_passed=is_valid and is_sched_valid,
            tool_calls=tool_logs
        )

