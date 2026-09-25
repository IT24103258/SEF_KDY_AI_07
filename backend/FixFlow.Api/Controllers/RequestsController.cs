using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    public RequestsController(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MaintenanceRequestSummaryDto>>>> GetRequests()
    {
        var requests = await _workOrderService.GetAvailableRequestsAsync();
        return Ok(ApiResponse<List<MaintenanceRequestSummaryDto>>.SuccessResult(requests));
    }
}
