using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SchedulingController : ControllerBase
{
    private readonly ISchedulingService _schedulingService;

    public SchedulingController(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpPost("requests/{requestId}/schedule")]
    public async Task<ActionResult<ApiResponse<ScheduleProposalDto>>> ScheduleRequest(
        Guid requestId,
        [FromBody] ScheduleRequestDto request)
    {
        request.RequestId = requestId;
        var proposal = await _schedulingService.CreateConflictFreeWorkOrderAsync(request, GetCurrentUserId());
        return Ok(ApiResponse<ScheduleProposalDto>.SuccessResult(proposal, "AI Scheduling proposal created successfully."));
    }

    [HttpPost("scheduling/validate")]
    public async Task<ActionResult<ApiResponse<ScheduleValidationResult>>> ValidateSchedule(
        [FromBody] ScheduleValidationRequestDto request)
    {
        var result = await _schedulingService.ValidateScheduleAsync(
            request.TechnicianId,
            request.StartTime,
            request.EndTime,
            request.DurationMinutes,
            request.Priority,
            request.SlaDeadline,
            request.ExcludeWorkOrderId);

        return Ok(ApiResponse<ScheduleValidationResult>.SuccessResult(result));
    }

    [HttpGet("scheduling/business-hours")]
    public async Task<ActionResult<ApiResponse<List<BusinessHoursDto>>>> GetBusinessHours()
    {
        var hours = await _schedulingService.GetBusinessHoursAsync();
        return Ok(ApiResponse<List<BusinessHoursDto>>.SuccessResult(hours));
    }
}

public class ScheduleValidationRequestDto
{
    public Guid TechnicianId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? SlaDeadline { get; set; }
    public Guid? ExcludeWorkOrderId { get; set; }
}

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ISchedulingService _schedulingService;

    public CalendarController(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CalendarEventDto>>>> GetCalendar(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? technicianId = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? status = null)
    {
        var events = await _schedulingService.GetCalendarEventsAsync(startDate, endDate, technicianId, priority, status);
        return Ok(ApiResponse<List<CalendarEventDto>>.SuccessResult(events));
    }
}

[ApiController]
[Route("api/reports")]
[Authorize]
public class WorkOrderReportsController : ControllerBase
{
    private readonly ISchedulingService _schedulingService;

    public WorkOrderReportsController(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    [HttpGet("work-orders")]
    public async Task<ActionResult<ApiResponse<SchedulingReportDto>>> GetWorkOrderReports(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var reports = await _schedulingService.GetSchedulingReportsAsync(startDate, endDate);
        return Ok(ApiResponse<SchedulingReportDto>.SuccessResult(reports));
    }

    [HttpGet("scheduling")]
    public async Task<ActionResult<ApiResponse<SchedulingReportDto>>> GetSchedulingReports(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var reports = await _schedulingService.GetSchedulingReportsAsync(startDate, endDate);
        return Ok(ApiResponse<SchedulingReportDto>.SuccessResult(reports));
    }
}
