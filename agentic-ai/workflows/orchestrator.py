import uuid
from fastapi import FastAPI, HTTPException, APIRouter
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from fastapi import APIRouter, HTTPException
from schemas.workflow_schemas import WorkflowExecutionRequest, WorkflowExecutionResult, StepExecutionResult
from agents.agent_skeletons import ClassificationAgent, PriorityAgent, AssignmentAgent, SchedulingAgent

app = FastAPI()
router = APIRouter()

'''
================================================================================
FIXFLOW SHARED FOUNDATION MULTI-AGENT ORCHESTRATOR
================================================================================
IMPORTANT FOR ALL 4 MEMBERS:
Register your step execution in your designated section inside execute_workflow().
Do NOT create separate orchestrator files.
================================================================================
'''
# ============================================================
# CORS MIDDLEWARE CONFIGURATION
# ============================================================
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ============================================================
# PYDANTIC SCHEMAS FOR DIRECT & STANDALONE REQUESTS
# ============================================================
class DirectAssignRequest(BaseModel):
    requestId: int | str
    technicianId: int | str

class StandaloneAssignmentRequest(BaseModel):
    request_id: int | str = "REQ-101"
    required_skill: str = "Electrical"
    priority: str = "High"

# ============================================================
# ROUTE HANDLERS & ENDPOINTS
# ============================================================

@router.get("/health")
def health_check():
    return {"status": "healthy", "service": "FixFlow Agentic AI Orchestrator"}

@app.post("/agent/assign")
def direct_assign(request: DirectAssignRequest):
    return {
        "status": "SUCCESS",
        "message": f"Technician {request.technicianId} assigned to request {request.requestId} via AI Orchestrator",
        "request_id": request.requestId,
        "technician_id": request.technicianId
    }

# ============================================================
# MEMBER 3 — DEDICATED STANDALONE TEST ENDPOINT
# ============================================================
@app.post("/api/agent/assignment/test")
def test_assignment_agent_only(request: StandaloneAssignmentRequest):

    assignment_agent = AssignmentAgent()
    
    step3_result = assignment_agent.run_step({
        "request_id": request.request_id,
        "required_skill": request.required_skill,
        "priority": request.priority
    })
    
    return {
        "status": "SUCCESS",
        "result": step3_result
    }

# ============================================================
# MAIN MULTI-AGENT WORKFLOW ORCHESTRATOR
# ============================================================

# ---------------------------------------------------------------------------
# Shared, auditable planning metadata. Each workflow type declares the ordered
# agent steps it actually executes, and the plan enumerates exactly those steps
# so the persisted audit trail never claims work that was not performed.
#
#   FullPipeline   → the integrated C1 → C2 → C3 → C4 application workflow.
#   Priority       → PriorityAgent only, so Component 2 can be tested/re-run
#                    without unrelated agents executing or affecting its result.
# ---------------------------------------------------------------------------
_STEP_PLAN = {
    "Classification": "ClassificationAgent — intake & classify the raw request facts.",
    "Priority": "PriorityAgent — deterministically assess risk & priority (Component 2).",
    "Assignment": "AssignmentAgent — match technician skills to the classified request.",
    "Scheduling": "SchedulingAgent — propose a conflict-free work-order schedule.",
}

_FULL_PIPELINE = ["Classification", "Priority", "Assignment", "Scheduling"]

_WORKFLOW_STEPS = {
    "FullPipeline": _FULL_PIPELINE,
    "Classification": ["Classification"],
    "Priority": ["Priority"],
    "Assignment": ["Assignment"],
    "Scheduling": ["Scheduling"],
}

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

    # ============================================================
    # SHARED — Objective & structured plan (auditable).
    # Additive metadata: every workflow declares its objective and the
    # ordered plan of the agent steps it will execute.
    # ============================================================
    workflow_type = getattr(request.workflow_type, "value", str(request.workflow_type))
    objective = _OBJECTIVES.get(workflow_type, _OBJECTIVES["FullPipeline"])
    selected_steps = _WORKFLOW_STEPS.get(workflow_type, _FULL_PIPELINE)
    plan = [f"{i}. {_STEP_PLAN[name]}" for i, name in enumerate(selected_steps, start=1)]

    step1_result = None
    step2_result = None
    step3_result = None

    # ============================================================
    # MEMBER 1 — REQUEST INTAKE & CLASSIFICATION AGENT STEP
    # ============================================================
    if "Classification" in selected_steps:
        classification_agent = ClassificationAgent()
        step1_result = classification_agent.run_step(request.input_context)
        steps.append(step1_result)

        if step1_result.status == "REQUIRES_HUMAN_APPROVAL":
            requires_approval = True
            approval_reason = "Member 1 Classification Agent triggered human review threshold."

    # Member 2 — Risk & Priority Assessment Agent Step
    if "Priority" in selected_steps:
        priority_context = dict(request.input_context)
        if step1_result is not None:
            priority_context.update(step1_result.output_data)

        priority_agent = PriorityAgent()
        step2_result = priority_agent.run_step(priority_context)
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
    if "Assignment" in selected_steps:
        assignment_context = dict(request.input_context)
        assignment_context["request_id"] = request.request_id
        if step1_result is not None:
            assignment_context["required_skill"] = step1_result.output_data.get("required_skill", "General")#previously was category, but now we are using required_skill for better matching
        if step2_result is not None:
            assignment_context["priority"] = step2_result.output_data.get("priority_level", "Normal")

        assignment_agent = AssignmentAgent()
        step3_result = assignment_agent.run_step(assignment_context)
        steps.append(step3_result)

    # Member 4 — Scheduling & Work Order Management Agent Step
    if "Scheduling" in selected_steps:
        scheduling_context = dict(request.input_context)
        scheduling_context["request_id"] = request.request_id
        if step3_result is not None:
            scheduling_context["assigned_technician_id"] = step3_result.output_data.get("top_match_id")
        if step2_result is not None:
            scheduling_context["priority"] = step2_result.output_data.get("priority_level", "Normal")
        if step1_result is not None:
            scheduling_context["category"] = step1_result.output_data.get("category", "General")
        scheduling_context["description"] = request.input_context.get("description", "")

        scheduling_agent = SchedulingAgent()
        step4_result = scheduling_agent.run_step(scheduling_context)
        steps.append(step4_result)

        if step4_result.status == "REQUIRES_HUMAN_APPROVAL":
            requires_approval = True
            approval_reason = "Member 4 Scheduling Agent requires manager approval for schedule proposal."

    # The workflow result is derived only from the agents this workflow type
    # actually executed, so an unrelated agent can never affect it.
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

app.include_router(router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("workflows.orchestrator:app", host="0.0.0.0", port=8001, reload=True)
