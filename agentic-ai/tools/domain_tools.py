from tools.base_tool import BaseTool
from typing import Dict, Any

class GetAssetDetailsTool(BaseTool):
    def __init__(self):
        super().__init__("get_asset_details", "Fetches technical specifications & criticality of an asset")

    def _run(self, asset_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "asset_id": asset_id,
            "name": "Main Server Room UPS",
            "criticality": "Critical",
            "category": "Electrical"
        }

class GetLocationDetailsTool(BaseTool):
    def __init__(self):
        super().__init__("get_location_details", "Fetches location building, floor, room and coordinates")

    def _run(self, location_id: str = "", **kwargs) -> Dict[str, Any]:
        return {
            "location_id": location_id,
            "building": "Block A",
            "room": "Lab A201",
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
            "skills": ["High Voltage Electrical Systems", "HVAC Chiller Maintenance"]
        }

# Tool Registry for Allow-Listing
ALLOW_LISTED_TOOLS = {
    "get_asset_details": GetAssetDetailsTool(),
    "get_location_details": GetLocationDetailsTool(),
    "get_issue_category_rules": GetIssueCategoryRulesTool(),
    "get_sla_config": GetSLAConfigTool(),
    "get_technician_skills": GetTechnicianSkillsTool()
}
