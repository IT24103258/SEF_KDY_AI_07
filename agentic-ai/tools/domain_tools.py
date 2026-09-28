<<<<<<< HEAD
from datetime import timedelta
from tools.base_tool import BaseTool
from typing import Dict, Any, Optional, List, Tuple
=======
"""
Component 2 — Domain Tools for Risk & Priority Assessment.

Design rules:
- Every tool must use *supplied input parameters first*.
- Realistic mock data is used ONLY when the supplied parameter is absent.
- No tool is allowed to hard-code a single constant and ignore the supplied context.
- save_risk_assessment / save_priority_assessment are stub tools that
  acknowledge the request; real persistence is performed by the ASP.NET
  PriorityAssessmentService (not by the Python agent).
"""

from tools.base_tool import BaseTool
from typing import Dict, Any, Optional


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _normalize_level(value: Optional[str]) -> Optional[str]:
    """Return a canonical level string or None if unrecognised."""
    if not value:
        return None
    mapping = {
        "critical": "Critical",
        "high": "High",
        "medium": "Medium",
        "low": "Low",
    }
    return mapping.get(value.strip().lower())


# ---------------------------------------------------------------------------
# Shared tools (used by multiple agents — do not break their interfaces)
# ---------------------------------------------------------------------------
>>>>>>> main

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
    """
    Authoritative SLA table — mirrors the C# implementation exactly.
    Low: 8h/48h | Medium: 4h/24h | High: 2h/8h | Critical: 1h/4h
    """
    def __init__(self):
        super().__init__("get_sla_config", "Fetches target response and resolution times for priority levels")

    def _run(self, priority_level: str = "Medium", **kwargs) -> Dict[str, Any]:
        sla_table = {
            "Critical": {"response_hours": 1, "resolution_hours": 4},
            "High":     {"response_hours": 2, "resolution_hours": 8},
            "Medium":   {"response_hours": 4, "resolution_hours": 24},
            "Low":      {"response_hours": 8, "resolution_hours": 48},
        }
        normalized = _normalize_level(priority_level) or "Medium"
        return sla_table.get(normalized, {"response_hours": 4, "resolution_hours": 24})


class GetTechnicianSkillsTool(BaseTool):
    def __init__(self):
        super().__init__("get_technician_skills", "Lists skills associated with technicians")

    def _run(self, technician_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "technician_id": technician_id,
            "skills": ["Residential Electrical Systems", "HVAC & AC Maintenance"]
        }


# ---------------------------------------------------------------------------
# Component 2-exclusive tools
# ---------------------------------------------------------------------------

class GetAssetCriticalityTool(BaseTool):
    """
    Returns the criticality of an asset.

    Priority of data:
    1. ``asset_criticality`` kwarg supplied by the caller (from input_context)
    2. ``asset_category`` fallback to category-based rules
    3. Realistic mock default when nothing is supplied

    IMPORTANT: This tool must NEVER return a hard-coded constant that ignores
    the caller's supplied context.
    """
    # Category → criticality mapping (mirrors C# AssessAssetCriticality)
    _CATEGORY_MAP = {
        "elevator/lift": "Critical",
        "electrical": "Critical",
        "fire safety": "Critical",
        "water supply": "High",
        "security": "High",
        "hvac": "Medium",
        "plumbing": "Medium",
        "lighting": "Low",
        "general": "Low",
    }

    def __init__(self):
        super().__init__("get_asset_criticality", "Assesses criticality level and operational importance of an asset")

    def _run(self, asset_id: str = "", asset_criticality: str = "", asset_category: str = "", **kwargs) -> Dict[str, Any]:
        # 1. Use explicitly supplied criticality override
        normalized = _normalize_level(asset_criticality)
        if normalized:
            return {
                "asset_id": asset_id,
                "criticality": normalized,
                "is_life_safety": normalized in ("Critical", "High"),
                "operational_impact": normalized,
                "source": "supplied_override"
            }

        # 2. Category-based rule
        cat_key = (asset_category or "").strip().lower()
        if cat_key in self._CATEGORY_MAP:
            crit = self._CATEGORY_MAP[cat_key]
            return {
                "asset_id": asset_id,
                "criticality": crit,
                "is_life_safety": crit in ("Critical", "High"),
                "operational_impact": crit,
                "source": "category_rule"
            }

        # 3. Deterministic fallback — mirrors C# AssessAssetCriticality, which
        # returns "Low" for an unrecognised category (never invents Medium/Critical).
        return {
            "asset_id": asset_id,
            "criticality": "Low",
            "is_life_safety": False,
            "operational_impact": "Low",
            "source": "category_default"
        }


class GetLocationRiskRulesTool(BaseTool):
    """
    Returns location hazard and density information.

    Priority of data:
    1. Structured kwargs supplied by the caller
    2. Realistic mock default — NOT forced to High occupancy
    """
    def __init__(self):
        super().__init__("get_location_risk_rules", "Fetches location hazard rules and environmental risk modifiers")

    def _run(
        self,
        location_id: str = "",
        is_high_risk_zone: Optional[bool] = None,
        occupancy_density: str = "",
        **kwargs
    ) -> Dict[str, Any]:
        # Use supplied density if provided
        density = _normalize_level(occupancy_density) or None
        if density is None:
            # Realistic default: Medium (not always High)
            density = "Medium"

        risk_zone = is_high_risk_zone if is_high_risk_zone is not None else False

        return {
            "location_id": location_id,
            "is_high_risk_zone": risk_zone,
            "environmental_risk": "High" if risk_zone else "Low",
            "occupancy_density": density,
            "source": "supplied_or_default"
        }


class GetOpenRequestsForAssetTool(BaseTool):
    def __init__(self):
        super().__init__("get_open_requests_for_asset", "Fetches open maintenance requests and active incidents for an asset")

    def _run(
        self,
        asset_id: str = "",
        open_request_count: int = -1,
        recent_failures_30d: int = -1,
        **kwargs
    ) -> Dict[str, Any]:
        # Use supplied counts if caller provides them; otherwise realistic defaults
        count = open_request_count if open_request_count >= 0 else 0
        failures = recent_failures_30d if recent_failures_30d >= 0 else 0
        return {
            "asset_id": asset_id,
            "open_request_count": count,
            "recent_failures_30d": failures,
        }


class GetRiskMatrixRulesTool(BaseTool):
    """
    Evaluates the 4×4 impact×likelihood matrix.

    IMPORTANT: Returns a risk_level derived from the SUPPLIED impact and
    likelihood values, not a hard-coded constant.

    Mirrors the C# DetermineRiskLevel thresholds:
      Critical×Critical = score 64 → Critical
      High×High         = score 36 → High
      Medium×Medium     = score 16 → Medium
      Low×Low           = score  4 → Low
    """
    _LEVEL_POINTS = {"Critical": 4, "High": 3, "Medium": 2, "Low": 1}
    _LEVEL_NAMES = {4: "Critical", 3: "High", 2: "Medium", 1: "Low"}

    def __init__(self):
        super().__init__("get_risk_matrix_rules", "Evaluates impact vs likelihood matrix to determine risk level and baseline priority")

    def _run(self, impact: str = "Medium", likelihood: str = "Medium", **kwargs) -> Dict[str, Any]:
        imp_pts = self._LEVEL_POINTS.get(_normalize_level(impact) or "Medium", 2)
        lik_pts = self._LEVEL_POINTS.get(_normalize_level(likelihood) or "Medium", 2)
        matrix_score = imp_pts * lik_pts  # 1..16

        # Map matrix_score to risk level (thresholds aligned with C# CalculateRiskScore)
        if matrix_score >= 12:
            risk_level = "Critical"
        elif matrix_score >= 6:
            risk_level = "High"
        elif matrix_score >= 3:
            risk_level = "Medium"
        else:
            risk_level = "Low"

        return {
            "impact": _normalize_level(impact) or impact,
            "likelihood": _normalize_level(likelihood) or likelihood,
            "matrix_score": matrix_score,
            "risk_level": risk_level,
            "suggested_priority": risk_level,
        }


class GetHistoricalRiskDataTool(BaseTool):
    """
    Returns historical recurrence data for an asset.

    Priority:
    1. Supplied ``historical_recurrence_rate`` kwarg
    2. Realistic mock default — Low recurrence (not always Moderate)
    """
    _RATE_MAP = {
        "critical": "Critical",
        "high": "High",
        "moderate": "Moderate",
        "medium": "Moderate",
        "low": "Low",
    }

    def __init__(self):
        super().__init__("get_historical_risk_data", "Fetches historical recurrence patterns and failure frequencies for risk calculation")

    def _run(
        self,
        asset_id: str = "",
        historical_recurrence_rate: str = "",
        mean_time_between_failures_days: int = -1,
        **kwargs
    ) -> Dict[str, Any]:
        rate = (
            self._RATE_MAP.get(historical_recurrence_rate.strip().lower())
            if historical_recurrence_rate
            else "Low"  # Realistic default — not Moderate
        )
        mtbf = mean_time_between_failures_days if mean_time_between_failures_days > 0 else 90

        return {
            "asset_id": asset_id,
            "historical_recurrence_rate": rate,
            "mean_time_between_failures_days": mtbf,
        }


class SaveRiskAssessmentTool(BaseTool):
    """
    Stub acknowledgement tool.

    The Python agent must NOT claim real database persistence — that is
    performed by the ASP.NET PriorityAssessmentService. This tool merely
    returns an acknowledgement so the agent step log is complete.
    """
    def __init__(self):
        super().__init__("save_risk_assessment", "Acknowledges risk assessment for agent step log; real persistence is in ASP.NET.")

    def _run(self, request_id: str = "", risk_score: int = 0, **kwargs) -> Dict[str, Any]:
        return {
            "status": "acknowledged",
            "request_id": request_id,
            "risk_score": risk_score,
            "note": "Real persistence is performed by ASP.NET PriorityAssessmentService; this acknowledgement is for agent step-log only.",
            "saved": False,  # Explicitly false — this tool does NOT write to a database
        }


class SavePriorityAssessmentTool(BaseTool):
    """
    Stub acknowledgement tool — see SaveRiskAssessmentTool.
    """
    def __init__(self):
        super().__init__("save_priority_assessment", "Acknowledges priority assessment for agent step log; real persistence is in ASP.NET.")

    def _run(self, request_id: str = "", priority: str = "Medium", **kwargs) -> Dict[str, Any]:
        return {
            "status": "acknowledged",
            "request_id": request_id,
            "priority": priority,
            "note": "Real persistence is performed by ASP.NET PriorityAssessmentService; this acknowledgement is for agent step-log only.",
            "saved": False,  # Explicitly false
        }

<<<<<<< HEAD
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
        from scheduling.slot_planner import parse_iso_to_aware, DAY_NAMES, IST

        conflicts = []
        validation_messages = []
        is_valid = True
        conflict_free = True
        within_bh = True
        sla_compliant = True

        if not start_time or not end_time:
            is_valid = False
            validation_messages.append("Start time and end time are required.")
            return {
                "technician_id": technician_id,
                "is_valid": is_valid,
                "conflict_free": conflict_free,
                "within_business_hours": within_bh,
                "within_technician_availability": is_technician_available,
                "sla_compliant": sla_compliant,
                "conflicts": conflicts,
                "validation_messages": validation_messages,
            }

        if duration_minutes <= 0:
            is_valid = False
            validation_messages.append("Duration must be a positive number of minutes.")

        if not is_technician_available:
            is_valid = False
            validation_messages.append("Technician is unavailable.")

        dt_start = parse_iso_to_aware(start_time)
        dt_end = parse_iso_to_aware(end_time)

        if dt_start and dt_end and dt_start >= dt_end:
            is_valid = False
            validation_messages.append("Start time must be earlier than end time.")
        elif not dt_start or not dt_end:
            if start_time >= end_time:
                is_valid = False
                validation_messages.append("Start time must be earlier than end time.")

        if dt_start and dt_end and duration_minutes > 0:
            expected_end = dt_start + timedelta(minutes=duration_minutes)
            actual_diff = abs((dt_end - expected_end).total_seconds())
            if actual_diff > 60:
                validation_messages.append(
                    f"Duration inconsistency: proposed span is {int((dt_end - dt_start).total_seconds() // 60)} min "
                    f"but duration_minutes is {duration_minutes}."
                )

        if dt_start and business_hours:
            bh = business_hours or {}
            working_days = bh.get("working_days", ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"])
            day_name = DAY_NAMES[dt_start.weekday()]

            if day_name in working_days:
                weekday_open = bh.get("weekday_open", "08:00:00")
                if day_name == "Saturday":
                    weekday_close = bh.get("saturday_close", "13:00:00")
                else:
                    weekday_close = bh.get("weekday_close", "17:00:00")

                open_parts = weekday_open.split(":")
                close_parts = weekday_close.split(":")
                biz_open = dt_start.replace(hour=int(open_parts[0]), minute=int(open_parts[1]) if len(open_parts) > 1 else 0, second=0, microsecond=0)
                biz_close = dt_start.replace(hour=int(close_parts[0]), minute=int(close_parts[1]) if len(close_parts) > 1 else 0, second=0, microsecond=0)

                if dt_start < biz_open or dt_end > biz_close:
                    within_bh = False
            else:
                within_bh = False

        bookings = existing_bookings or []
        for b in bookings:
            b_start = b.get("start_time") or b.get("start")
            b_end = b.get("end_time") or b.get("end")
            if b_start and b_end:
                b_dt_start = parse_iso_to_aware(b_start) if isinstance(b_start, str) else b_start
                b_dt_end = parse_iso_to_aware(b_end) if isinstance(b_end, str) else b_end

                has_overlap = False
                if b_dt_start and b_dt_end and dt_start and dt_end:
                    has_overlap = b_dt_start < dt_end and b_dt_end > dt_start
                elif b_start and b_end:
                    has_overlap = b_start < end_time and b_end > start_time

                if has_overlap:
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

        if sla_deadline:
            dt_sla = parse_iso_to_aware(sla_deadline)
            if dt_sla and dt_end:
                if dt_end > dt_sla:
                    sla_compliant = False
                    is_valid = False
                    validation_messages.append(f"Proposed completion time ({end_time}) breaches SLA deadline ({sla_deadline}).")
            elif end_time and end_time > sla_deadline:
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
            "validation_messages": validation_messages,
        }


# Tool Registry for Allow-Listing
=======

# ---------------------------------------------------------------------------
# Tool Registry / Allow-List
# ---------------------------------------------------------------------------

>>>>>>> main
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
<<<<<<< HEAD
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
=======
>>>>>>> main
}
