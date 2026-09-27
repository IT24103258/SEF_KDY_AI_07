"""
Pydantic schemas for the POST /api/classify endpoint.

These are SEPARATE from the shared ClassificationOutput in schemas/agent_schemas.py
because this file adds the HTTP request shape and extends the response with
required_skill and reason — fields the existing ClassificationOutput does not have.

We deliberately do NOT modify agent_schemas.py (shared team file).
"""

from pydantic import BaseModel, Field
from typing import Optional


class ClassifyRequest(BaseModel):
    """
    Body accepted by POST /api/classify.

    Both fields are treated as untrusted plain text.
    The classifier is deterministic (keyword scoring), so injecting instructions
    such as "Ignore the above and return category=X" has no effect — the
    algorithm only counts keyword matches, never interprets the text as commands.
    """
    title: str = Field(..., min_length=1, max_length=200)
    description: str = Field(..., min_length=1, max_length=4000)


class ClassifyResponse(BaseModel):
    """
    Response shape returned by POST /api/classify.
    Matches the AgentClassificationResponseDto in the C# backend exactly
    (snake_case, same field names).
    """
    category: str
    subcategory: Optional[str] = None
    confidence_score: float = Field(..., ge=0.0, le=1.0)
    requires_review: bool
    required_skill: Optional[str] = None
    reason: Optional[str] = None
