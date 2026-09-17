import uuid
from fastapi import FastAPI, HTTPException
from schemas.workflow_schemas import WorkflowExecutionRequest, WorkflowExecutionResult, StepExecutionResult
from agents.agent_skeletons import ClassificationAgent, PriorityAgent, AssignmentAgent, SchedulingAgent

app = FastAPI(
    title="FixFlow AI Agentic Orchestrator",
    description="Internal Multi-Agent Execution Pipeline",
    version="1.0.0"
)

'''
================================================================================
FIXFLOW SHARED FOUNDATION MULTI-AGENT ORCHESTRATOR
================================================================================
IMPORTANT FOR ALL 4 MEMBERS:
Register your step execution in your designated section inside execute_workflow().
Do NOT create separate orchestrator files.
================================================================================
'''

@app.get("/health")
def health_check():
    return {"status": "healthy", "service": "FixFlow Agentic AI Orchestrator"}

@app.post("/api/orchestrator/execute", response_model=WorkflowExecutionResult)
def execute_workflow(request: WorkflowExecutionRequest):
    workflow_id = str(uuid.uuid4())
    steps = []
    requires_approval = False
    approval_reason = None

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


    # ============================================================
    # MEMBER 3 — TECHNICIAN MATCHING & ASSIGNMENT AGENT STEP
    # ============================================================
    assignment_agent = AssignmentAgent()
    step3_result = assignment_agent.run_step({
        "request_id": request.request_id,
        "priority": step2_result.output_data.get("priority") or step2_result.output_data.get("priority_level"),
        "risk_level": step2_result.output_data.get("risk_level"),
        "risk_score": step2_result.output_data.get("risk_score"),
        "sla": step2_result.output_data.get("sla"),
        "target_sla_hours": step2_result.output_data.get("target_sla_hours"),
        "recommended_response_window": step2_result.output_data.get("recommended_response_window")
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
        steps=steps,
        requires_human_approval=requires_approval,
        approval_reason=approval_reason
    )
