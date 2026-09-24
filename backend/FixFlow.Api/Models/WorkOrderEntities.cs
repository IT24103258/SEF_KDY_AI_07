using FixFlow.Api.Models.Enums;

namespace FixFlow.Api.Models;

public class WorkOrder : BaseEntity
{
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid RequestId { get; set; }
    public MaintenanceRequest? Request { get; set; }

    public Guid TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;

    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public DateTime? SLADeadline { get; set; }

    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalComments { get; set; } = string.Empty;

    public bool ConflictDetected { get; set; } = false;
    public string ConflictDetailsJson { get; set; } = "[]";
    public string AiDecisionSummary { get; set; } = string.Empty;

    public ICollection<WorkOrderStatusHistory> StatusHistories { get; set; } = new List<WorkOrderStatusHistory>();
    public ICollection<WorkNote> Notes { get; set; } = new List<WorkNote>();
    public ICollection<CompletionEvidence> Evidence { get; set; } = new List<CompletionEvidence>();
    public ICollection<ScheduleProposal> Proposals { get; set; } = new List<ScheduleProposal>();
}

public class WorkOrderStatusHistory : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public WorkOrderStatus PreviousStatus { get; set; }
    public WorkOrderStatus NewStatus { get; set; }

    public Guid? ChangedById { get; set; }
    public User? ChangedBy { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class WorkNote : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public string NoteText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CompletionEvidence : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public string FileKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }

    public string SignatureDataUrl { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class ScheduleProposal : BaseEntity
{
    public Guid? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid RequestId { get; set; }
    public MaintenanceRequest? Request { get; set; }

    public Guid TechnicianId { get; set; }
    public Technician? Technician { get; set; }

    public DateTime ProposedStartTime { get; set; }
    public DateTime ProposedEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }

    public WorkOrderPriority Priority { get; set; }
    public DateTime? SlaDeadline { get; set; }

    public bool ConflictDetected { get; set; }
    public string ConflictDetailsJson { get; set; } = "[]";
    public string DecisionSummary { get; set; } = string.Empty;
    public string ValidationDetailsJson { get; set; } = "{}";

    public bool IsAccepted { get; set; } = false;
}

public class BusinessHours : BaseEntity
{
    public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
    public string DayName { get; set; } = string.Empty;
    public TimeSpan OpenTime { get; set; } = new TimeSpan(8, 0, 0);
    public TimeSpan CloseTime { get; set; } = new TimeSpan(17, 0, 0);
    public bool IsWorkingDay { get; set; } = true;
}
