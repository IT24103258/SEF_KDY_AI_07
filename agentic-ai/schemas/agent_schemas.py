from pydantic import BaseModel, Field
from typing import List, Optional

# Member 1 Schema
class ClassificationOutput(BaseModel):
    category: str
    subcategory: str
    confidence_score: float = Field(..., ge=0.0, le=1.0)
    requires_review: bool = False

# Member 2 Schema
class PriorityOutput(BaseModel):
    risk_score: int = Field(..., ge=1, le=100)
    priority_level: str # Low, Medium, High, Critical
    target_sla_hours: int
    hazard_flag: bool = False

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
