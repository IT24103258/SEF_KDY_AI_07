using System.Security.Claims;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentWorkflowsController : ControllerBase
{
    private readonly IAgentWorkflowService _workflowService;

    public AgentWorkflowsController(IAgentWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AgentWorkflowDto>>>> GetWorkflows()
    {
        var workflows = await _workflowService.GetWorkflowsAsync();
        return Ok(ApiResponse<List<AgentWorkflowDto>>.SuccessResult(workflows));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AgentWorkflowDto>>> GetWorkflowById(Guid id)
    {
        var workflow = await _workflowService.GetWorkflowByIdAsync(id);
        return Ok(ApiResponse<AgentWorkflowDto>.SuccessResult(workflow));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Manager")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ApprovalActionDto>>>> GetPendingApprovals()
    {
        var approvals = await _approvalService.GetPendingApprovalsAsync();
        return Ok(ApiResponse<List<ApprovalActionDto>>.SuccessResult(approvals));
    }

    [HttpPost("{id}/decide")]
    public async Task<ActionResult<ApiResponse<ApprovalActionDto>>> ProcessApproval(Guid id, [FromBody] ApprovalDecisionDto decision)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var approverId)) return Unauthorized();

        var result = await _approvalService.ProcessApprovalAsync(id, decision, approverId);
        return Ok(ApiResponse<ApprovalActionDto>.SuccessResult(result, "Human-in-the-loop approval decision recorded successfully."));
    }
}
