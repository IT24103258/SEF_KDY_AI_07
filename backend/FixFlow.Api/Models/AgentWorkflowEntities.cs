using FixFlow.Api.Models.Enums;

namespace FixFlow.Api.Models;

public class AgentWorkflow : BaseEntity
{
    public Guid RequestId { get; set; }
    public MaintenanceRequest? Request { get; set; }
    public string WorkflowType { get; set; } = string.Empty; // FullPipeline, Classification, Priority, Assignment, Scheduling
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string OutputSummaryJson { get; set; } = string.Empty;

    public ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();
    public ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();
}

public class AgentStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public AgentWorkflow? Workflow { get; set; }
    public string AgentName { get; set; } = string.Empty; // ClassificationAgent, PriorityAgent, AssignmentAgent, SchedulingAgent
    public string StepName { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public string InputDataJson { get; set; } = string.Empty;
    public string OutputDataJson { get; set; } = string.Empty;
    public string ValidationResultJson { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<AgentToolCall> ToolCalls { get; set; } = new List<AgentToolCall>();
}

public class AgentToolCall : BaseEntity
{
    public Guid StepId { get; set; }
    public AgentStep? Step { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class ApprovalAction : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public AgentWorkflow? Workflow { get; set; }
    public Guid? ApproverId { get; set; }
    public User? Approver { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string ReasonRequired { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
