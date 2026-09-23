using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PriorityAgentController : ControllerBase
{
    private readonly IPriorityAgentService _priorityAgentService;
    private readonly ILogger<PriorityAgentController> _logger;

    public PriorityAgentController(
        IPriorityAgentService priorityAgentService,
        ILogger<PriorityAgentController> logger)
    {
        _priorityAgentService = priorityAgentService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates risk & priority using Component 2 Python PriorityAgent and persists the result to PostgreSQL.
    /// </summary>
    [HttpPost("requests/{id}/priority-agent")]
    [HttpPost("requests/{id}/priority-agent/assess")]
    public async Task<ActionResult<ApiResponse<PriorityAssessmentDto>>> AssessPriorityWithAgent(
        Guid id,
        [FromBody] PriorityAgentEvaluationRequestDto? dto)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "PriorityAgent";
        var result = await _priorityAgentService.EvaluateAndPersistAsync(id, dto, $"PriorityAgent ({userEmail})");
        return Ok(ApiResponse<PriorityAssessmentDto>.SuccessResult(result, "Priority assessment evaluated by PriorityAgent and persisted successfully."));
    }

    /// <summary>
    /// Previews PriorityAgent calculated output without persisting to the database.
    /// </summary>
    [HttpPost("requests/{id}/priority-agent/preview")]
    public async Task<ActionResult<ApiResponse<PriorityAgentResultDto>>> PreviewPriorityWithAgent(
        Guid id,
        [FromBody] PriorityAgentEvaluationRequestDto? dto)
    {
        var result = await _priorityAgentService.EvaluateAgentAsync(id, dto);
        return Ok(ApiResponse<PriorityAgentResultDto>.SuccessResult(result, "PriorityAgent calculation completed (preview mode)."));
    }
}
