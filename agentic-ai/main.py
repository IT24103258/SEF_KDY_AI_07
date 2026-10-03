"""
FixFlow Agentic AI Service — unified FastAPI entry point.

Start with:
    uvicorn main:app --host 0.0.0.0 --port 8000 --reload

This is the ONLY entry point that exposes every route the ASP.NET Core backend
calls, because it composes the whole service:

    POST /api/classify              ← Component 1 (ClassificationAgentService)
    POST /api/orchestrator/execute  ← Component 2 (PriorityAgentService) via the
                                       included orchestrator router
    GET  /health

Starting uvicorn with ``workflows.orchestrator:app`` instead serves only the
orchestrator's own application object, which never had /api/classify registered
on it — that is what produced the 404 seen by the C# ClassificationAgentService.
"""
import sys
import os

# Allow imports from the agentic-ai root when running via uvicorn from any cwd.
# This must run before the project imports below, otherwise ``uvicorn main:app``
# only resolves them when the current directory already happens to be agentic-ai.
sys.path.insert(0, os.path.dirname(__file__))

from workflows.orchestrator import router as orchestrator_router

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
app.include_router(orchestrator_router)

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
