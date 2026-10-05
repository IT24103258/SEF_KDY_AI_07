namespace FixFlow.Api.Models.Enums;

public enum WorkOrderStatus
{
    Draft = 0,
    Proposed = 1,
    PendingManagerApproval = 2,
    Approved = 3,
    Rejected = 4,
    RevisionRequested = 5,
    Scheduled = 6,
    InProgress = 7,
    Paused = 8,
    Completed = 9,
    Cancelled = 10,
    Failed = 11
}

public enum WorkOrderPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
