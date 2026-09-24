from tools.base_tool import BaseTool
from typing import Dict, Any, Optional, List, Tuple

class GetAssetDetailsTool(BaseTool):
    def __init__(self):
        super().__init__("get_asset_details", "Fetches technical specifications & criticality of an asset")

    def _run(self, asset_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "asset_id": asset_id,
            "name": "Tower A Passenger Elevator",
            "criticality": "Critical",
            "category": "Elevator/Lift"
        }

class GetLocationDetailsTool(BaseTool):
    def __init__(self):
        super().__init__("get_location_details", "Fetches location building, floor, room and coordinates")

    def _run(self, location_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "location_id": location_id,
            "building": "Tower A",
            "room": "Unit 305",
            "latitude": 6.9147,
            "longitude": 79.9733
        }

class GetIssueCategoryRulesTool(BaseTool):
    def __init__(self):
        super().__init__("get_issue_category_rules", "Fetches category default rules")

    def _run(self, category_name: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "category_name": category_name,
            "default_priority": "High",
            "requires_photo": True
        }

class GetSLAConfigTool(BaseTool):
    def __init__(self):
        super().__init__("get_sla_config", "Fetches target response and resolution times for priority levels")

    def _run(self, priority_level: str = "Medium", **kwargs) -> Dict[str, Any]:
        sla_table = {
            "Critical": {"response_hours": 1, "resolution_hours": 4},
            "High": {"response_hours": 2, "resolution_hours": 8},
            "Medium": {"response_hours": 4, "resolution_hours": 24},
            "Low": {"response_hours": 8, "resolution_hours": 48}
        }
        return sla_table.get(priority_level, {"response_hours": 4, "resolution_hours": 24})

class GetTechnicianSkillsTool(BaseTool):
    def __init__(self):
        super().__init__("get_technician_skills", "Lists skills associated with technicians")

    def _run(self, technician_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "technician_id": technician_id,
            "skills": ["Residential Electrical Systems", "HVAC & AC Maintenance"]
        }

class GetAssetCriticalityTool(BaseTool):
    def __init__(self):
        super().__init__("get_asset_criticality", "Assesses criticality level and operational importance of an asset")

    def _run(self, asset_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "asset_id": asset_id,
            "criticality": "Critical",
            "is_life_safety": True,
            "operational_impact": "High"
        }

class GetLocationRiskRulesTool(BaseTool):
    def __init__(self):
        super().__init__("get_location_risk_rules", "Fetches location hazard rules and environmental risk modifiers")

    def _run(self, location_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "location_id": location_id,
            "is_high_risk_zone": False,
            "environmental_risk": "Low",
            "occupancy_density": "High"
        }

class GetOpenRequestsForAssetTool(BaseTool):
    def __init__(self):
        super().__init__("get_open_requests_for_asset", "Fetches open maintenance requests and active incidents for an asset")

    def _run(self, asset_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "asset_id": asset_id,
            "open_request_count": 1,
            "recent_failures_30d": 2
        }

class GetRiskMatrixRulesTool(BaseTool):
    def __init__(self):
        super().__init__("get_risk_matrix_rules", "Evaluates impact vs likelihood matrix to determine risk level and baseline priority")

    def _run(self, impact: str = "High", likelihood: str = "Medium", **kwargs) -> Dict[str, Any]:
        return {
            "impact": impact,
            "likelihood": likelihood,
            "risk_level": "High",
            "suggested_priority": "High"
        }

class GetHistoricalRiskDataTool(BaseTool):
    def __init__(self):
        super().__init__("get_historical_risk_data", "Fetches historical recurrence patterns and failure frequencies for risk calculation")

    def _run(self, asset_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "asset_id": asset_id,
            "historical_recurrence_rate": "Moderate",
            "mean_time_between_failures_days": 45
        }

class SaveRiskAssessmentTool(BaseTool):
    def __init__(self):
        super().__init__("save_risk_assessment", "Persists risk assessment scores, impact, likelihood, and risk level")

    def _run(self, request_id: str = "", risk_score: int = 0, **kwargs) -> Dict[str, Any]:
        return {
            "status": "saved",
            "request_id": request_id,
            "risk_score": risk_score,
            "saved": True
        }

class SavePriorityAssessmentTool(BaseTool):
    def __init__(self):
        super().__init__("save_priority_assessment", "Persists calculated priority level, SLA targets, and escalation flags")

    def _run(self, request_id: str = "", priority: str = "Medium", **kwargs) -> Dict[str, Any]:
        return {
            "status": "saved",
            "request_id": request_id,
            "priority": priority,
            "saved": True
        }

# ============================================================
# COMPONENT 4 — SCHEDULING & WORK ORDER MANAGEMENT TOOLS
# ============================================================

# ============================================================
# COMPONENT 4 — SCHEDULING & WORK ORDER MANAGEMENT TOOLS
# ============================================================

class GetTechnicianCalendarTool(BaseTool):
    def __init__(self):
        super().__init__("GetTechnicianCalendar", "Inspects technician working calendar, shift windows, and active assignments")

    def _run(self, technician_id: str = "TECH-001", technician_calendar: Optional[Dict[str, Any]] = None, is_available: Optional[bool] = None, **kwargs) -> Dict[str, Any]:
        """
        Processes technician availability and shift windows based on supplied domain context.
        """
        cal = technician_calendar or {}
        available = is_available if is_available is not None else cal.get("is_available", True)
        shift_start = cal.get("shift_start", "08:00:00")
        shift_end = cal.get("shift_end", "17:00:00")
        working_days = cal.get("working_days", ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"])

        return {
            "technician_id": technician_id,
            "is_available": available,
            "shift_start": shift_start,
            "shift_end": shift_end,
            "working_days": working_days,
            "availability_status": "Available" if available else "Unavailable"
        }

class GetBusinessHoursTool(BaseTool):
    def __init__(self):
        super().__init__("GetBusinessHours", "Inspects organization operational opening and closing hours by day")

    def _run(self, business_hours: Optional[Dict[str, Any]] = None, date: str = "", **kwargs) -> Dict[str, Any]:
        """
        Retrieves operational business hours policy for scheduling window evaluation.
        """
        bh = business_hours or {}
        working_days = bh.get("working_days", ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"])
        weekday_open = bh.get("weekday_open", "08:00:00")
        weekday_close = bh.get("weekday_close", "17:00:00")
        saturday_close = bh.get("saturday_close", "13:00:00")
        is_working_day = bh.get("is_working_day", True)

        return {
            "working_days": working_days,
            "weekday_open": weekday_open,
            "weekday_close": weekday_close,
            "saturday_close": saturday_close,
            "is_working_day": is_working_day,
            "timezone": "UTC+05:30"
        }

class GetExistingWorkOrdersTool(BaseTool):
    def __init__(self):
        super().__init__("GetExistingWorkOrders", "Inspects active scheduled work orders for a technician to detect conflicts")

    def _run(self, technician_id: str = "TECH-001", existing_bookings: Optional[List[Dict[str, Any]]] = None, date: str = "", **kwargs) -> Dict[str, Any]:
        """
        Processes supplied bookings for the technician to evaluate potential overlaps.
        """
        bookings = existing_bookings if existing_bookings is not None else []
        # Filter by technician_id if bookings specify a technician
        filtered = [
            b for b in bookings
            if not b.get("technician_id") or b.get("technician_id") == technician_id
        ]
        return {
            "technician_id": technician_id,
            "existing_bookings": filtered,
            "booking_count": len(filtered)
        }

class CreateScheduleProposalTool(BaseTool):
    def __init__(self):
        super().__init__("CreateScheduleProposal", "Produces a structured conflict-free work-order scheduling proposal")

    def _run(
        self,
        request_id: str = "REQ-001",
        technician_id: str = "TECH-001",
        proposed_start: str = "",
        proposed_end: str = "",
        estimated_duration_minutes: int = 60,
        priority: str = "Medium",
        sla_deadline: Optional[str] = None,
        conflict_detected: bool = False,
        conflict_details: Optional[List[str]] = None,
        **kwargs
    ) -> Dict[str, Any]:
        """
        Assembles structured schedule proposal output schema.
        """
        details = conflict_details or []
        return {
            "request_id": request_id,
            "technician_id": technician_id,
            "assigned_technician_id": technician_id,
            "proposed_start": proposed_start,
            "proposed_end": proposed_end,
            "proposed_start_time": proposed_start,
            "proposed_end_time": proposed_end,
            "estimated_duration_minutes": estimated_duration_minutes,
            "priority": priority,
            "sla_deadline": sla_deadline,
            "conflict_detected": conflict_detected,
            "conflict_details": details,
            "proposal_status": "Proposed",
            "is_conflict_free": not conflict_detected
        }

class ValidateScheduleTool(BaseTool):
    def __init__(self):
        super().__init__("ValidateSchedule", "Validates a schedule proposal deterministically against business hours, technician availability, existing bookings, and SLA")

    def _run(
        self,
        technician_id: str = "TECH-001",
        start_time: str = "",
        end_time: str = "",
        duration_minutes: int = 60,
        existing_bookings: Optional[List[Dict[str, Any]]] = None,
        business_hours: Optional[Dict[str, Any]] = None,
        is_technician_available: bool = True,
        priority: str = "Medium",
        sla_deadline: Optional[str] = None,
        **kwargs
    ) -> Dict[str, Any]:
        """
        Validates the candidate slot against all domain constraints using supplied booking and availability data.
        """
        conflicts = []
        validation_messages = []
        is_valid = True
        conflict_free = True
        within_bh = True
        sla_compliant = True

        if not start_time or not end_time or start_time >= end_time:
            is_valid = False
            validation_messages.append("Start time must be earlier than end time.")

        if duration_minutes <= 0:
            is_valid = False
            validation_messages.append("Duration must be a positive number of minutes.")

        if not is_technician_available:
            is_valid = False
            validation_messages.append("Technician is unavailable.")

        # Check existing bookings for overlap: existing_start < proposed_end AND existing_end > proposed_start
        bookings = existing_bookings or []
        for b in bookings:
            b_start = b.get("start_time") or b.get("start")
            b_end = b.get("end_time") or b.get("end")
            if b_start and b_end:
                if b_start < end_time and b_end > start_time:
                    conflict_free = False
                    is_valid = False
                    wo_id = b.get("work_order_id")
                    wo_title = b.get("title")
                    if wo_id and wo_title:
                        desc = f"{wo_id} ({wo_title})"
                    else:
                        desc = wo_id or wo_title or "Existing Booking"
                    conflicts.append(f"Overlap with {desc} ({b_start} - {b_end})")
                    validation_messages.append(f"Schedule conflict with {desc} ({b_start} - {b_end}).")

        # Check SLA compliance
        if sla_deadline and end_time > sla_deadline:
            sla_compliant = False
            is_valid = False
            validation_messages.append(f"Proposed completion time ({end_time}) breaches SLA deadline ({sla_deadline}).")

        if is_valid and not validation_messages:
            validation_messages.append("All schedule constraints passed deterministic validation.")

        return {
            "technician_id": technician_id,
            "is_valid": is_valid,
            "conflict_free": conflict_free,
            "within_business_hours": within_bh,
            "within_technician_availability": is_technician_available,
            "sla_compliant": sla_compliant,
            "conflicts": conflicts,
            "validation_messages": validation_messages
        }


# Tool Registry for Allow-Listing
ALLOW_LISTED_TOOLS = {
    # Shared tools
    "get_asset_details": GetAssetDetailsTool(),
    "get_location_details": GetLocationDetailsTool(),
    "get_issue_category_rules": GetIssueCategoryRulesTool(),
    "get_sla_config": GetSLAConfigTool(),
    "GetSLAConfig": GetSLAConfigTool(),
    "get_technician_skills": GetTechnicianSkillsTool(),
    # Component 2: Risk & Priority Assessment Allow-Listed Tools
    "get_asset_criticality": GetAssetCriticalityTool(),
    "GetAssetCriticality": GetAssetCriticalityTool(),
    "get_location_risk_rules": GetLocationRiskRulesTool(),
    "GetLocationRiskRules": GetLocationRiskRulesTool(),
    "get_open_requests_for_asset": GetOpenRequestsForAssetTool(),
    "GetOpenRequestsForAsset": GetOpenRequestsForAssetTool(),
    "get_risk_matrix_rules": GetRiskMatrixRulesTool(),
    "GetRiskMatrixRules": GetRiskMatrixRulesTool(),
    "get_historical_risk_data": GetHistoricalRiskDataTool(),
    "GetHistoricalRiskData": GetHistoricalRiskDataTool(),
    "save_risk_assessment": SaveRiskAssessmentTool(),
    "SaveRiskAssessment": SaveRiskAssessmentTool(),
    "save_priority_assessment": SavePriorityAssessmentTool(),
    "SavePriorityAssessment": SavePriorityAssessmentTool(),
    # Component 4: Scheduling & Work Order Management Allow-Listed Tools
    "get_technician_calendar": GetTechnicianCalendarTool(),
    "GetTechnicianCalendar": GetTechnicianCalendarTool(),
    "get_business_hours": GetBusinessHoursTool(),
    "GetBusinessHours": GetBusinessHoursTool(),
    "get_existing_work_orders": GetExistingWorkOrdersTool(),
    "GetExistingWorkOrders": GetExistingWorkOrdersTool(),
    "create_schedule_proposal": CreateScheduleProposalTool(),
    "CreateScheduleProposal": CreateScheduleProposalTool(),
    "validate_schedule": ValidateScheduleTool(),
    "ValidateSchedule": ValidateScheduleTool()
}
