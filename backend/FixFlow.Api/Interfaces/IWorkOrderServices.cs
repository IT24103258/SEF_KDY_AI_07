using FixFlow.Api.DTOs;

namespace FixFlow.Api.Interfaces;

public interface IWorkOrderService
{
    Task<PagedResultDto<WorkOrderSummaryDto>> GetWorkOrdersAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? status = null,
        Guid? technicianId = null,
        string? priority = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortBy = null,
        string? sortDirection = null,
        Guid? currentUserId = null,
        string? currentUserRole = null);

    Task<WorkOrderDto> GetWorkOrderByIdAsync(Guid id, Guid? currentUserId = null, string? currentUserRole = null);
    Task<WorkOrderDto> CreateWorkOrderAsync(WorkOrderCreateDto dto, Guid creatorUserId);
    Task<WorkOrderDto> UpdateWorkOrderAsync(Guid id, WorkOrderUpdateDto dto, Guid updaterUserId);
    Task<bool> DeleteWorkOrderAsync(Guid id, Guid deleterUserId);
    Task<WorkOrderDto> UpdateStatusAsync(Guid id, WorkOrderStatusUpdateDto dto, Guid currentUserId, string currentUserRole);
    Task<WorkOrderDto> ApproveWorkOrderAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId);
    Task<WorkOrderDto> RejectWorkOrderAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId);
    Task<WorkOrderDto> RequestRevisionAsync(Guid id, WorkOrderApprovalDecisionDto decision, Guid approverUserId);
    Task<WorkNoteDto> AddNoteAsync(Guid workOrderId, WorkNoteCreateDto dto, Guid authorUserId);
    Task<WorkOrderDto> CompleteWorkOrderAsync(Guid id, CompleteWorkOrderDto dto, Guid technicianUserId);
    Task<List<WorkOrderSummaryDto>> GetPendingApprovalsAsync();
    Task<List<WorkOrderSummaryDto>> GetTechnicianScheduleAsync(Guid technicianUserId, DateTime? filterDate = null);
}

public interface ISchedulingService
{
    Task<ScheduleProposalDto> CreateConflictFreeWorkOrderAsync(ScheduleRequestDto request, Guid requesterUserId);
    Task<ScheduleValidationResult> ValidateScheduleAsync(
        Guid technicianId,
        DateTime startTime,
        DateTime endTime,
        int durationMinutes,
        string priority,
        DateTime? slaDeadline = null,
        Guid? excludeWorkOrderId = null);
    Task<List<CalendarEventDto>> GetCalendarEventsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? technicianId = null,
        string? priority = null,
        string? status = null);
    Task<SchedulingReportDto> GetSchedulingReportsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<BusinessHoursDto>> GetBusinessHoursAsync();
}

public class BusinessHoursDto
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
}
