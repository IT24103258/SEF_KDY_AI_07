from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from enum import Enum

class WorkflowTypeEnum(str, Enum):
    FULL_PIPELINE = "FullPipeline"
    CLASSIFICATION = "Classification"
    PRIORITY = "Priority"
    ASSIGNMENT = "Assignment"
    SCHEDULING = "Scheduling"

class WorkflowExecutionRequest(BaseModel):
    request_id: str
    workflow_type: WorkflowTypeEnum = WorkflowTypeEnum.FULL_PIPELINE
    input_context: Dict[str, Any] = Field(default_factory=dict)

class ToolCallLog(BaseModel):
    tool_name: str
    input_params: Dict[str, Any]
    output_params: Dict[str, Any]
    execution_time_ms: int
    success: bool = True
    error_message: Optional[str] = None

class StepExecutionResult(BaseModel):
    agent_name: str
    step_name: str
    status: str # SUCCESS, FAILED, REQUIRES_HUMAN_APPROVAL
    output_data: Dict[str, Any]
    validation_passed: bool
    tool_calls: List[ToolCallLog] = Field(default_factory=list)

class WorkflowExecutionResult(BaseModel):
    workflow_id: str
    request_id: str
    status: str
    steps: List[StepExecutionResult] = Field(default_factory=list)
    requires_human_approval: bool = False
    approval_reason: Optional[str] = None
