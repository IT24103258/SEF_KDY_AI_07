import uuid
import typing
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from schemas.workflow_schemas import (
    WorkflowExecutionRequest,
    WorkflowExecutionResult,
    StepExecutionResult,
)
from agents.agent_skeletons import (
    ClassificationAgent,
    PriorityAgent,
    AssignmentAgent,
    SchedulingAgent,
)

router = APIRouter()

"""
================================================================================
FIXFLOW SHARED FOUNDATION MULTI-AGENT ORCHESTRATOR
================================================================================
IMPORTANT FOR ALL 4 MEMBERS:
Register your step execution in your designated section inside execute_workflow().
Do NOT create separate orchestrator files.
================================================================================
"""

# ============================================================
# PYDANTIC SCHEMAS FOR DIRECT & STANDALONE REQUESTS
# ============================================================
class DirectAssignRequest(BaseModel):
    requestId: typing.Union[int, str]
    technicianId: typing.Union[int, str]


class StandaloneAssignmentRequest(BaseModel):
    request_id: typing.Union[int, str] = "REQ-101"
    required_skill: str = "Electrical"
    priority: str = "High"


# ============================================================
# ROUTE HANDLERS & ENDPOINTS
# ============================================================

@router.get("/health")
def health_check():
    return {"status": "healthy", "service": "FixFlow Agentic AI Orchestrator"}


@router.post("/agent/assign")
def direct_assign(request: DirectAssignRequest):
    return {
        "status": "SUCCESS",
        "message": f"Technician {request.technicianId} assigned to request {request.requestId} via AI Orchestrator",
        "request_id": request.requestId,
        "technician_id": request.technicianId,
    }


# ============================================================
# MEMBER 3 — DEDICATED STANDALONE TEST ENDPOINT
# ============================================================
@router.post("/api/agent/assignment/test")
def test_assignment_agent_only(request: StandaloneAssignmentRequest):
    assignment_agent = AssignmentAgent()

    step3_result = assignment_agent.run_step(
        {
            "request_id": request.request_id,
            "required_skill": request.required_skill,
            "priority": request.priority,
        }
    )

    return {"status": "SUCCESS", "result": step3_result}


# ============================================================
# MAIN MULTI-AGENT WORKFLOW ORCHESTRATOR
# ============================================================

# ---------------------------------------------------------------------------
# Shared, auditable planning metadata. The orchestrator always executes the
# full agent pipeline; the objective/plan make the workflow's intent explicit
# and are persisted by the ASP.NET layer for auditability.
# ---------------------------------------------------------------------------
_PLAN = [
    "1. ClassificationAgent — intake & classify the raw request facts.",
    "2. PriorityAgent — assess risk & SLA urgency.",
    "3. SchedulingAgent — assign technician & slot.",
]

_OBJECTIVES = {
    "FullPipeline": "Run the full FixFlow multi-agent pipeline: classify, assess risk & priority, assign a technician, and schedule the work order.",
    "Classification": "Classify the incoming maintenance request into a category and subcategory with a confidence score.",
    "Priority": "Deterministically assess risk & priority for the request (Component 2), producing an auditable, validated assessment or a safe failure flagged for downstream human review.",
    "Assignment": "Match the most suitable technician to the request based on required skills and workload.",
    "Scheduling": "Propose a conflict-free schedule for the assigned technician and work order.",
}


@router.post("/api/orchestrator/execute", response_model=WorkflowExecutionResult)
def execute_workflow(request: WorkflowExecutionRequest):
    workflow_id = str(uuid.uuid4())
    steps = []
    requires_approval = False
    approval_reason = None

    # Step 1: Classification Agent
    classification_agent = ClassificationAgent()
    step1_res = classification_agent.run_step({"request": request})
    steps.append(step1_res)

    # Step 2: Priority Agent
    priority_agent = PriorityAgent()
    step2_res = priority_agent.run_step({"request": request, "classification": step1_res})
    steps.append(step2_res)

    # Step 3: Assignment & Scheduling Agent
    scheduling_agent = SchedulingAgent()
    step3_res = scheduling_agent.run_step({"request": request, "priority": step2_res})
    steps.append(step3_res)

    return WorkflowExecutionResult(
        workflow_id=workflow_id,
        status="COMPLETED",
        steps=steps,
        requires_approval=requires_approval,
        approval_reason=approval_reason,
    )
    # ============================================================
    # SHARED — Objective & structured plan (auditable).
    # Additive metadata: every workflow declares its objective and the
    # ordered plan of agent steps the pipeline will execute.
    # ============================================================
    workflow_type = getattr(request.workflow_type, "value", str(request.workflow_type))
    objective = _OBJECTIVES.get(workflow_type, _OBJECTIVES["FullPipeline"])
    plan = list(_PLAN)

    # ============================================================
    # MEMBER 1 — REQUEST INTAKE & CLASSIFICATION AGENT STEP
    # ============================================================
    classification_agent = ClassificationAgent()
    step1_result = classification_agent.run_step(request.input_context)
    steps.append(step1_result)

    if step1_result.status == "REQUIRES_HUMAN_APPROVAL":
        requires_approval = True
        approval_reason = "Member 1 Classification Agent triggered human review threshold."

    # Member 2 — Risk & Priority Assessment Agent Step
    priority_agent = PriorityAgent()
    step2_result = priority_agent.run_step({**request.input_context, **step1_result.output_data})
    steps.append(step2_result)

    if step2_result.status == "REQUIRES_HUMAN_APPROVAL":
        requires_approval = True
        approval_reason = "Member 2 Priority Agent triggered critical risk approval."
    elif step2_result.status == "FAILED":
        # Safe failure: a tool failure or invalid assessment must be flagged for
        # downstream human review, never silently completed.
        requires_approval = True
        approval_reason = (
            "Member 2 Priority Agent safe-failure (tool failure or invalid assessment). "
            "Flagged for downstream human review."
        )

    # Member 3 — Technician Matching & Assignment Agent Step
    assignment_agent = AssignmentAgent()
    step3_result = assignment_agent.run_step({
        "request_id": request.request_id,
        "required_skill": step1_result.output_data.get("category", "General"),
        "priority": step2_result.output_data.get("priority_level", "Normal")
    })
    steps.append(step3_result)

    # Member 4 — Scheduling & Work Order Management Agent Step
    scheduling_agent = SchedulingAgent()
    step4_result = scheduling_agent.run_step({
        "request_id": request.request_id,
        "assigned_technician_id": step3_result.output_data.get("top_match_id")
    })
    steps.append(step4_result)

    final_status = "WAITING_FOR_HUMAN_APPROVAL" if requires_approval else "COMPLETED"

    return WorkflowExecutionResult(
        workflow_id=workflow_id,
        request_id=request.request_id,
        status=final_status,
        objective=objective,
        plan=plan,
        workflow_type=workflow_type,
        steps=steps,
        requires_human_approval=requires_approval,
        approval_reason=approval_reason
    )