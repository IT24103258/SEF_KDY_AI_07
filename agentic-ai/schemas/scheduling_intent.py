from pydantic import BaseModel, Field
from typing import List, Optional
from enum import Enum


class PreferenceType(str, Enum):
    EXACT = "exact"
    MORNING = "morning"
    AFTERNOON = "afternoon"
    EARLIEST = "earliest"
    FLEXIBLE = "flexible"
    CUSTOM = "custom"


class UrgencyLevel(str, Enum):
    URGENT = "urgent"
    HIGH = "high"
    NORMAL = "normal"
    LOW = "low"


class FlexibilityLevel(str, Enum):
    STRICT = "strict"
    MODERATE = "moderate"
    FLEXIBLE = "flexible"


class SchedulingIntent(BaseModel):
    preference_type: PreferenceType = PreferenceType.FLEXIBLE
    preferred_start: Optional[str] = None
    preferred_end: Optional[str] = None
    preferred_period: Optional[str] = None
    urgency: UrgencyLevel = UrgencyLevel.NORMAL
    avoid_periods: List[str] = Field(default_factory=list)
    flexibility: FlexibilityLevel = FlexibilityLevel.FLEXIBLE
    requested_date: Optional[str] = None
    requested_duration_minutes: Optional[int] = Field(default=None, ge=1)
    interpretation_summary: str = ""
    confidence: float = Field(default=0.5, ge=0.0, le=1.0)
