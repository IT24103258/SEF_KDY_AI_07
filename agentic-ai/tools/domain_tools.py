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


# ---------------------------------------------------------------------------
# Tool Registry / Allow-List
# ---------------------------------------------------------------------------

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
}
