using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PrioritiesController : ControllerBase
{
    private readonly IPriorityAssessmentService _priorityService;
    private readonly ILogger<PrioritiesController> _logger;

    public PrioritiesController(IPriorityAssessmentService priorityService, ILogger<PrioritiesController> logger)
    {
        _priorityService = priorityService;
        _logger = logger;
    }

    /// <summary>
    /// 1. Get priority assessment for a maintenance request
    /// </summary>
    [HttpGet("priorities/{requestId}")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> GetPriority(Guid requestId)
    {
        var result = await _priorityService.GetPriorityByRequestIdAsync(requestId);
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result));
    }

    /// <summary>
    /// 2. Create priority assessment for a maintenance request
    /// </summary>
    [HttpPost("requests/{id}/priority-assessments")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> CreatePriorityAssessment(
        Guid id,
        [FromBody] CreatePriorityAssessmentDto? dto)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "PriorityService";
        var result = await _priorityService.CreatePriorityAssessmentAsync(id, dto, userEmail);
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result, "Priority assessment evaluated and recorded successfully."));
    }

    /// <summary>
    /// 3. Update priority assessment (Manager override)
    /// </summary>
    [HttpPut("priority-assessments/{id}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> UpdatePriorityAssessment(
        Guid id,
        [FromBody] UpdatePriorityAssessmentDto dto)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Manager";
        var result = await _priorityService.UpdatePriorityAssessmentAsync(id, dto, userEmail);
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result, "Priority assessment updated successfully."));
    }

    /// <summary>
    /// 4. Get list of priority assessments with paging
    /// </summary>
    [HttpGet("priority-assessments")]
    public async Task<ActionResult<ApiResponse<PagedResult<PriorityAssessmentDto>>>> GetPriorityAssessments(
        [FromQuery] PriorityAssessmentSearchFilterDto filter)
    {
        var result = await _priorityService.GetPriorityAssessmentsAsync(filter);
        return Ok(ApiResponse<PagedResult<PriorityAssessmentDto>>.SuccessResult(result));
    }

    /// <summary>
    /// 5. Search priority assessments with query filters
    /// </summary>
    [HttpGet("priority-assessments/search")]
    public async Task<ActionResult<ApiResponse<PagedResult<PriorityAssessmentDto>>>> SearchPriorityAssessments(
        [FromQuery] PriorityAssessmentSearchFilterDto filter)
    {
        var result = await _priorityService.SearchPriorityAssessmentsAsync(filter);
        return Ok(ApiResponse<PagedResult<PriorityAssessmentDto>>.SuccessResult(result));
    }

    /// <summary>
    /// 6. Trigger risk escalation for a maintenance request
    /// </summary>
    [HttpPost("requests/{id}/escalate")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> EscalateRequest(
        Guid id,
        [FromBody] EscalateRequestDto dto)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "User";
        var result = await _priorityService.EscalateRequestAsync(id, dto, userEmail);
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result, "Request escalated successfully."));
    }

    /// <summary>
    /// 7. Retrieve detailed risk assessment breakdown and factors
    /// </summary>
    [HttpGet("requests/{id}/risk-assessment")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> GetRiskAssessment(Guid id)
    {
        var result = await _priorityService.GetRiskAssessmentAsync(id);
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result));
    }

    /// <summary>
    /// 8. Simulate risk score and escalation scenarios (read-only sandbox)
    /// </summary>
    [HttpGet("requests/{id}/risk-simulation")]
    public async Task<ActionResult<ApiResponse<RiskSimulationResultDto>>> SimulateRisk(
        Guid id,
        [FromQuery] RiskSimulationRequestDto dto)
    {
        var result = await _priorityService.SimulateRiskEscalationAsync(id, dto);
        return Ok(ApiResponse<RiskSimulationResultDto>.SuccessResult(result, "Risk simulation generated successfully."));
    }
}
