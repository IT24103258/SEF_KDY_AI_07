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
    risk_level: str # Low, Medium, High, Critical
    priority: str # Low, Medium, High, Critical
    recommended_response_window: str = "Within 4 hours"
    sla: Dict[str, Any] = Field(default_factory=dict)
    escalation_flag: bool = False
    explanation: str = ""
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

# Member 4 Schema - Scheduling & Work Order Management
class ScheduleProposal(BaseModel):
    request_id: str
    assigned_technician_id: str = ""
    technician_id: Optional[str] = None
    proposed_start_time: str = ""
    proposed_end_time: str = ""
    proposed_start: Optional[str] = None
    proposed_end: Optional[str] = None
    estimated_duration_minutes: int = 60
    priority: str = "Medium"
    sla_deadline: Optional[str] = None
    conflict_detected: bool = False
    conflict_details: List[str] = Field(default_factory=list)
    within_business_hours: bool = True
    within_technician_availability: bool = True
    sla_compliant: bool = True
    proposal_status: str = "Proposed"
    decision_summary: str = "Selected an available technician slot within business hours and before the SLA deadline."
    validation_required: bool = True
    is_conflict_free: bool = True
