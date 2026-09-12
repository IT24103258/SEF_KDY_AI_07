from abc import ABC, abstractmethod
from typing import List, Dict, Any
from tools.base_tool import BaseTool
from schemas.workflow_schemas import StepExecutionResult, ToolCallLog
from validators.deterministic_validator import DeterministicValidator

class BaseAgent(ABC):
    def __init__(self, name: str, allowed_tools: List[BaseTool]):
        self.name = name
        self.allowed_tools = {tool.name: tool for tool in allowed_tools}

    def execute_tool(self, tool_name: str, **kwargs) -> ToolCallLog:
        if tool_name not in self.allowed_tools:
            return ToolCallLog(
                tool_name=tool_name,
                input_params=kwargs,
                output_params={},
                execution_time_ms=0,
                success=False,
                error_message=f"Unauthorized tool invocation: '{tool_name}' is not in allowed list for {self.name}"
            )
        
        res = self.allowed_tools[tool_name].execute(**kwargs)
        return ToolCallLog(
            tool_name=res["tool_name"],
            input_params=kwargs,
            output_params=res["output"],
            execution_time_ms=res["execution_time_ms"],
            success=res["success"],
            error_message=res["error"]
        )

    @abstractmethod
    def run_step(self, input_context: Dict[str, Any]) -> StepExecutionResult:
        pass
