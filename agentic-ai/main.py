"""
FixFlow Agentic AI Service — Component 1: Request Intake & Classification
FastAPI entry point.

Start with:
    uvicorn main:app --host 0.0.0.0 --port 8000 --reload

The C# ClassificationAgentService posts to POST /api/classify and expects
the ClassifyResponse JSON shape defined in classification/schemas.py.
"""

import sys
import os

# Allow imports from the agentic-ai root when running via uvicorn from any cwd
sys.path.insert(0, os.path.dirname(__file__))

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from classification.schemas import ClassifyRequest, ClassifyResponse
from classification.classifier import classify_request

app = FastAPI(
    title="FixFlow Classification Agent",
    description=(
        "Deterministic keyword-scoring classifier for apartment maintenance requests. "
        "Part of Component 1 — Request Intake & Classification."
    ),
    version="1.0.0",
)

# Allow the ASP.NET Core backend to call this service from localhost
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5000", "http://localhost:5001"],
    allow_methods=["POST", "GET"],
    allow_headers=["*"],
)


@app.get("/health")
def health_check():
    """Liveness probe — C# ClassificationAgentService can ping this."""
    return {"status": "ok", "service": "fixflow-classification-agent"}


@app.post("/api/classify", response_model=ClassifyResponse)
def classify(request: ClassifyRequest):
    """
    Classify a maintenance request.

    Accepts title + description as plain text (both treated as untrusted data).
    Returns category, subcategory, confidence_score, requires_review,
    required_skill, and a human-readable reason.

    The classifier is purely deterministic (keyword regex scoring).
    No model download, no external API call, no installation beyond
    what is already in requirements.txt.
    """
    try:
        return classify_request(request.title, request.description)
    except Exception as exc:  # pragma: no cover — unexpected runtime error
        raise HTTPException(status_code=500, detail=str(exc)) from exc
