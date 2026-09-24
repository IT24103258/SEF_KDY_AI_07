using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IFileStorageService _fileStorageService;

    public WorkOrdersController(
        IWorkOrderService workOrderService,
        IFileStorageService fileStorageService)
    {
        _workOrderService = workOrderService;
        _fileStorageService = fileStorageService;
    }

    private Guid GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Requester";
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<WorkOrderSummaryDto>>>> GetWorkOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? technicianId = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        var result = await _workOrderService.GetWorkOrdersAsync(
            page, pageSize, search, status, technicianId, priority, startDate, endDate, sortBy, sortDirection,
            GetCurrentUserId(), GetCurrentUserRole());

        return Ok(ApiResponse<PagedResultDto<WorkOrderSummaryDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GetWorkOrderById(Guid id)
    {
        var result = await _workOrderService.GetWorkOrderByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CreateWorkOrder([FromBody] WorkOrderCreateDto dto)
    {
        var result = await _workOrderService.CreateWorkOrderAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetWorkOrderById), new { id = result.Id }, ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> UpdateWorkOrder(Guid id, [FromBody] WorkOrderUpdateDto dto)
    {
        var result = await _workOrderService.UpdateWorkOrderAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteWorkOrder(Guid id)
    {
        var result = await _workOrderService.DeleteWorkOrderAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<bool>.SuccessResult(result, "Work order deleted/cancelled successfully."));
    }

    [HttpPost("{id}/status")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> UpdateStatus(Guid id, [FromBody] WorkOrderStatusUpdateDto dto)
    {
        var result = await _workOrderService.UpdateStatusAsync(id, dto, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order status updated successfully."));
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> ApproveWorkOrder(Guid id, [FromBody] WorkOrderApprovalDecisionDto decision)
    {
        decision.Approved = true;
        var result = await _workOrderService.ApproveWorkOrderAsync(id, decision, GetCurrentUserId());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order approved and scheduled successfully."));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> RejectWorkOrder(Guid id, [FromBody] WorkOrderApprovalDecisionDto decision)
    {
        decision.Approved = false;
        var result = await _workOrderService.RejectWorkOrderAsync(id, decision, GetCurrentUserId());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order rejected by Manager."));
    }

    [HttpPost("{id}/request-revision")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> RequestRevision(Guid id, [FromBody] WorkOrderApprovalDecisionDto decision)
    {
        decision.RequestRevision = true;
        var result = await _workOrderService.RequestRevisionAsync(id, decision, GetCurrentUserId());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Revision requested for schedule proposal."));
    }

    [HttpPost("{id}/notes")]
    public async Task<ActionResult<ApiResponse<WorkNoteDto>>> AddNote(Guid id, [FromBody] WorkNoteCreateDto dto)
    {
        var result = await _workOrderService.AddNoteAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<WorkNoteDto>.SuccessResult(result, "Work note added successfully."));
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CompleteWorkOrder(Guid id, [FromBody] CompleteWorkOrderDto dto)
    {
        var result = await _workOrderService.CompleteWorkOrderAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<WorkOrderDto>.SuccessResult(result, "Work order completed and customer sign-off recorded successfully."));
    }

    [HttpGet("pending-approval")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderSummaryDto>>>> GetPendingApprovals()
    {
        var result = await _workOrderService.GetPendingApprovalsAsync();
        return Ok(ApiResponse<List<WorkOrderSummaryDto>>.SuccessResult(result));
    }

    [HttpGet("technician/my-schedule")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderSummaryDto>>>> GetMySchedule([FromQuery] DateTime? filterDate = null)
    {
        var result = await _workOrderService.GetTechnicianScheduleAsync(GetCurrentUserId(), filterDate);
        return Ok(ApiResponse<List<WorkOrderSummaryDto>>.SuccessResult(result));
    }

    [HttpPost("{id}/evidence/photo")]
    [Authorize(Roles = "Administrator,Technician")]
    public async Task<ActionResult<ApiResponse<string>>> UploadPhoto(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.FailureResult("No file provided."));

        using var stream = file.OpenReadStream();
        var fileKey = await _fileStorageService.UploadFileAsync(stream, file.FileName);
        return Ok(ApiResponse<string>.SuccessResult(fileKey, "Photo uploaded successfully."));
    }
}
