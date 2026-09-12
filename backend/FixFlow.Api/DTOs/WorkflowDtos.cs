namespace FixFlow.Api.DTOs;

public class AgentWorkflowDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<AgentStepDto> Steps { get; set; } = new();
    public List<ApprovalActionDto> Approvals { get; set; } = new();
}

public class AgentStepDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InputDataJson { get; set; } = string.Empty;
    public string OutputDataJson { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<AgentToolCallDto> ToolCalls { get; set; } = new();
}

public class AgentToolCallDto
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public bool Success { get; set; }
}

public class ApprovalActionDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReasonRequired { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? ApproverName { get; set; }
}

public class ApprovalDecisionDto
{
    public bool Approved { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int ActiveWorkflows { get; set; }
    public int CompletedWorkOrders { get; set; }
    public int HighRiskEscalations { get; set; }
}
