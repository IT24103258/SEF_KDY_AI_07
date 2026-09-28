import os
from typing import Dict, Any

import requests

from tools.base_tool import BaseTool


class Component2BackendClient:
    """
    Component 2-only HTTP client.

    This client communicates with the ASP.NET backend rather than directly
    accessing PostgreSQL. The ASP.NET backend remains responsible for
    persistence and business rules.
    """

    def __init__(self):
        self.base_url = os.getenv(
            "FIXFLOW_API_BASE_URL",
            "http://localhost:5000"
        ).rstrip("/")

        self.timeout = int(
            os.getenv("FIXFLOW_API_TIMEOUT_SECONDS", "30")
        )

    def create_priority_assessment(
        self,
        request_id: str,
        asset_criticality: str,
        impact: str,
        likelihood: str,
        has_safety_hazard: bool,
        disruption_scope: str,
        notes: str = ""
    ) -> Dict[str, Any]:

        url = (
            f"{self.base_url}"
            f"/api/requests/{request_id}/priority-assessments"
        )

        payload = {
            "assetCriticalityOverride": asset_criticality,
            "impactOverride": impact,
            "likelihoodOverride": likelihood,
            "hasSafetyHazard": has_safety_hazard,
            "disruptionScope": disruption_scope,
            "notes": notes
        }

        try:
            response = requests.post(
                url,
                json=payload,
                timeout=self.timeout
            )

            if response.status_code >= 400:
                return {
                    "status": "failed",
                    "saved": False,
                    "request_id": request_id,
                    "http_status": response.status_code,
                    "error": response.text
                }

            data = response.json()

            return {
                "status": "saved",
                "saved": True,
                "request_id": request_id,
                "http_status": response.status_code,
                "data": data
            }

        except requests.RequestException as exc:
            return {
                "status": "failed",
                "saved": False,
                "request_id": request_id,
                "error": str(exc)
            }


class SaveRiskAssessmentBackendTool(BaseTool):
    """
    Component 2 persistence adapter.

    The actual persistence is performed by the existing ASP.NET
    PriorityAssessmentService.
    """

    def __init__(self):
        super().__init__(
            "save_risk_assessment_backend",
            "Persists Component 2 risk assessment through the ASP.NET backend."
        )

    def _run(
        self,
        request_id: str = "",
        risk_score: int = 0,
        asset_criticality: str = "Medium",
        impact: str = "Medium",
        likelihood: str = "Medium",
        has_safety_hazard: bool = False,
        disruption_scope: str = "Medium",
        notes: str = "",
        **kwargs
    ) -> Dict[str, Any]:

        client = Component2BackendClient()

        result = client.create_priority_assessment(
            request_id=request_id,
            asset_criticality=asset_criticality,
            impact=impact,
            likelihood=likelihood,
            has_safety_hazard=has_safety_hazard,
            disruption_scope=disruption_scope,
            notes=notes
        )

        result["risk_score"] = risk_score

        return result