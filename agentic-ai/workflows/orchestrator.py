import uuid
from fastapi import APIRouter, HTTPException
from schemas.workflow_schemas import WorkflowExecutionRequest, WorkflowExecutionResult, StepExecutionResult
from agents.agent_skeletons import ClassificationAgent, PriorityAgent, AssignmentAgent, SchedulingAgent

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

@router.get("/health")
def health_check():
    return {"status": "healthy", "service": "FixFlow Agentic AI Orchestrator"}


# ---------------------------------------------------------------------------
# Shared, auditable planning metadata. The orchestrator always executes the
# full agent pipeline; the objective/plan make the workflow's intent explicit
# and are persisted by the ASP.NET layer for auditability.
# ---------------------------------------------------------------------------
_PLAN = [
    "1. ClassificationAgent — intake & classify the raw request facts.",
    "2. PriorityAgent — deterministically assess risk & priority (Component 2).",
    "3. AssignmentAgent — match technician skills to the classified request.",
    "4. SchedulingAgent — propose a conflict-free work-order schedule.",
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


    # ============================================================
    # MEMBER 2 — RISK & PRIORITY ASSESSMENT AGENT STEP
    # ============================================================
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


    # ============================================================
    # MEMBER 3 — TECHNICIAN MATCHING & ASSIGNMENT AGENT STEP
    # ============================================================
    assignment_agent = AssignmentAgent()
    step3_result = assignment_agent.run_step({
        "request_id": request.request_id,
        "required_skill": step1_result.output_data.get("category", "General"),
        "priority": step2_result.output_data.get("priority_level", "Normal")
    })
    steps.append(step3_result)


    # ============================================================
    # MEMBER 4 — SCHEDULING & WORK ORDER MANAGEMENT AGENT STEP
    # ============================================================
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
