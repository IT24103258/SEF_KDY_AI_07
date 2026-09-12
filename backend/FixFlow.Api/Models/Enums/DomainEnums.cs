namespace FixFlow.Api.Models.Enums;

public enum RequestStatus
{
    Submitted = 0,
    InReview = 1,
    Classified = 2,
    PriorityAssigned = 3,
    Matched = 4,
    Scheduled = 5,
    InProgress = 6,
    Completed = 7,
    Cancelled = 8
}

public enum WorkflowStatus
{
    Pending = 0,
    Running = 1,
    WaitingForApproval = 2,
    Approved = 3,
    Rejected = 4,
    Completed = 5,
    Failed = 6
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum PriorityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
