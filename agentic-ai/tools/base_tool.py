import time
from abc import ABC, abstractmethod
from typing import Dict, Any


class BaseTool(ABC):
    def __init__(self, name: str, description: str):
        self.name = name
        self.description = description

    def execute(self, **kwargs) -> Dict[str, Any]:
        start_time = time.perf_counter()

        try:
            result = self._run(**kwargs)

            elapsed_ms = (time.perf_counter() - start_time) * 1000

            # Keep the existing integer millisecond format,
            # but prevent very fast executions from appearing as 0 ms.
            exec_time_ms = max(1, round(elapsed_ms))

            print(
                f"[TOOL TIMING] {self.name}: "
                f"{elapsed_ms:.6f} ms -> stored as {exec_time_ms} ms"
            )

            return {
                "tool_name": self.name,
                "output": result,
                "execution_time_ms": exec_time_ms,
                "success": True,
                "error": None
            }

        except Exception as e:
            elapsed_ms = (time.perf_counter() - start_time) * 1000
            exec_time_ms = max(1, round(elapsed_ms))

            print(
                f"[TOOL TIMING] {self.name}: "
                f"{elapsed_ms:.6f} ms -> stored as {exec_time_ms} ms"
            )

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