import uuid
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
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

@app.get("/health")
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
@app.post("/api/orchestrator/execute", response_model=WorkflowExecutionResult)
def execute_workflow(request: WorkflowExecutionRequest):
    workflow_id = str(uuid.uuid4())
    steps = []
    requires_approval = False
    approval_reason = None

    # Member 1 — Request Intake & Classification Agent Step
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
        steps=steps,
        requires_human_approval=requires_approval,
        approval_reason=approval_reason
    )