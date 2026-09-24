using FixFlow.Api.Models.Enums;

namespace FixFlow.Api.DTOs;

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;

    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string TechnicianEmployeeId { get; set; } = string.Empty;
    public string TechnicianSpecialization { get; set; } = string.Empty;

    public Guid? LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }

    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public DateTime? SLADeadline { get; set; }

    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string ApprovalComments { get; set; } = string.Empty;

    public bool ConflictDetected { get; set; }
    public string ConflictDetailsJson { get; set; } = "[]";
    public string AiDecisionSummary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<WorkOrderStatusHistoryDto> StatusHistories { get; set; } = new();
    public List<WorkNoteDto> Notes { get; set; } = new();
    public List<CompletionEvidenceDto> Evidence { get; set; } = new();
}

public class WorkOrderSummaryDto
{
    public Guid Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public DateTime? SLADeadline { get; set; }
    public bool ConflictDetected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkOrderCreateDto
{
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public Guid? LocationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;
    public DateTime? SLADeadline { get; set; }
}

public class WorkOrderUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;
    public Guid? LocationId { get; set; }
    public Guid? TechnicianId { get; set; }
}

public class WorkOrderStatusUpdateDto
{
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class WorkOrderApprovalDecisionDto
{
    public bool Approved { get; set; }
    public string Comments { get; set; } = string.Empty;
    public bool RequestRevision { get; set; } = false;
    public string RevisionNotes { get; set; } = string.Empty;
}

public class WorkNoteCreateDto
{
    public string NoteText { get; set; } = string.Empty;
}

public class WorkNoteDto
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class WorkOrderStatusHistoryDto
{
    public Guid Id { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class CompletionEvidenceDto
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string? UploadedByName { get; set; }
    public string SignatureDataUrl { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class CompleteWorkOrderDto
{
    public string SignerName { get; set; } = string.Empty;
    public string SignatureDataUrl { get; set; } = string.Empty;
    public string? CompletionNotes { get; set; }
    public string? PhotoFileKey { get; set; }
    public string? PhotoOriginalFileName { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
