from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any

# Member 1 Schema
class ClassificationOutput(BaseModel):
    category: str
    subcategory: str
    confidence_score: float = Field(..., ge=0.0, le=1.0)
    requires_review: bool = False

# Member 2 Schema - Risk & Priority Assessment
class PriorityOutput(BaseModel):
    asset_criticality: str = "Medium"
    impact_level: str = "Medium"
    likelihood_level: str = "Medium"
    risk_score: int = Field(..., ge=1, le=100)
    risk_level: str  # Low, Medium, High, Critical
    priority: str    # Low, Medium, High, Critical
    recommended_response_window: str = "Within 4 hours"
    sla: Dict[str, Any] = Field(default_factory=dict)
    escalation_flag: bool = False
    explanation: str = ""
    # Human-approval workflow (Component 2 downstream signal for Component 4)
    human_approval_required: Optional[bool] = False
    approval_reason: Optional[str] = None
    # Component 2 derived decision flags
    hazard_detected: Optional[bool] = False
    # Deterministic contributing factors (mirrors C# ContributingFactorsDto) so the
    # ASP.NET layer can re-verify the score before persisting.
    contributing_factors: Optional[Dict[str, Any]] = None
    # Backward compatibility fields
    priority_level: Optional[str] = None
    target_sla_hours: Optional[int] = None
    hazard_flag: Optional[bool] = False

# Member 3 Schema
class TechnicianCandidate(BaseModel):
    technician_id: str
    name: str
    match_score: float
    distance_km: float
    current_workload: int

class AssignmentOutput(BaseModel):
    request_id: str
    recommended_candidates: List[TechnicianCandidate]
    top_match_id: str

# Member 4 Schema
class ScheduleProposal(BaseModel):
    request_id: str
    assigned_technician_id: str
    proposed_start_time: str
    proposed_end_time: str
    is_conflict_free: bool
