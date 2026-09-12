import time
from abc import ABC, abstractmethod
from typing import Dict, Any

class BaseTool(ABC):
    def __init__(self, name: str, description: str):
        self.name = name
        self.description = description

    def execute(self, **kwargs) -> Dict[str, Any]:
        start_time = time.time()
        try:
            result = self._run(**kwargs)
            exec_time_ms = int((time.time() - start_time) * 1000)
            return {
                "tool_name": self.name,
                "output": result,
                "execution_time_ms": exec_time_ms,
                "success": True,
                "error": None
            }
        except Exception as e:
            exec_time_ms = int((time.time() - start_time) * 1000)
            return {
                "tool_name": self.name,
                "output": {},
                "execution_time_ms": exec_time_ms,
                "success": False,
                "error": str(e)
            }

    @abstractmethod
    def _run(self, **kwargs) -> Dict[str, Any]:
        pass
