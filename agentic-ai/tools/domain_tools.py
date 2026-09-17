from tools.base_tool import BaseTool
from typing import Dict, Any

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
    "SavePriorityAssessment": SavePriorityAssessmentTool()
}
