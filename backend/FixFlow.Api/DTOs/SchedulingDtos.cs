namespace FixFlow.Api.DTOs;

public class ScheduleRequestDto
{
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public DateTime? PreferredStartTime { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 60;
    public string Priority { get; set; } = "Medium";
    public DateTime? SlaDeadline { get; set; }
    public string RequesterNotes { get; set; } = string.Empty;
}

public class ScheduleProposalDto
{
    public Guid? ProposalId { get; set; }
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime ProposedStart { get; set; }
    public DateTime ProposedEnd { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? SlaDeadline { get; set; }

    public bool ConflictDetected { get; set; }
    public List<ConflictDetailDto> ConflictDetails { get; set; } = new();

    public bool WithinBusinessHours { get; set; }
    public bool WithinTechnicianAvailability { get; set; }
    public bool SlaCompliant { get; set; }
    public string ProposalStatus { get; set; } = "Proposed";
    public string DecisionSummary { get; set; } = string.Empty;
    public bool ValidationRequired { get; set; } = true;

    public ValidationChecklistDto ValidationChecklist { get; set; } = new();
    public Guid? WorkOrderId { get; set; }
}

public class ValidationChecklistDto
{
    public bool TechnicianAvailable { get; set; } = true;
    public bool ExistingBookingsChecked { get; set; } = true;
    public bool SlaRequirementPassed { get; set; } = true;
    public bool ScheduleConflictNone { get; set; } = true;
    public bool BusinessHoursValid { get; set; } = true;
    public bool RequiredSkillValid { get; set; } = true;
    public bool SchemaValid { get; set; } = true;
}

public class ConflictDetailDto
{
    public Guid? ExistingWorkOrderId { get; set; }
    public string ExistingTitle { get; set; } = string.Empty;
    public DateTime ConflictingStart { get; set; }
    public DateTime ConflictingEnd { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ScheduleValidationResult
{
    public bool IsValid { get; set; }
    public bool IsConflictFree { get; set; }
    public bool IsWithinBusinessHours { get; set; }
    public bool IsWithinTechnicianAvailability { get; set; }
    public bool IsSlaCompliant { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<ConflictDetailDto> Conflicts { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class CalendarEventDto
{
    public Guid Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool ConflictDetected { get; set; }
}

public class SchedulingReportDto
{
    public int TotalWorkOrders { get; set; }
    public int ScheduledCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int ConflictCount { get; set; }
    public int SlaBreachedCount { get; set; }
    public double SlaComplianceRate { get; set; }
    public double AverageSchedulingLeadTimeHours { get; set; }
    public List<StatusDistributionDto> StatusBreakdown { get; set; } = new();
    public List<PriorityDistributionDto> PriorityBreakdown { get; set; } = new();
    public List<TechnicianWorkloadDto> TechnicianWorkloads { get; set; } = new();
    public List<CompletionTrendDto> CompletionTrends { get; set; } = new();
}

public class StatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class PriorityDistributionDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TechnicianWorkloadDto
{
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public int AssignedJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int InProgressJobs { get; set; }
}

public class CompletionTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
    public int ScheduledCount { get; set; }
}
