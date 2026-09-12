using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class AgentWorkflowService : IAgentWorkflowService
{
    private readonly FixFlowDbContext _context;

    public AgentWorkflowService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<AgentWorkflowDto>> GetWorkflowsAsync()
    {
        return await _context.AgentWorkflows
            .Include(w => w.Request)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.ApprovalActions)
                .ThenInclude(a => a.Approver)
            .OrderByDescending(w => w.StartedAt)
            .Select(w => MapWorkflowToDto(w))
            .ToListAsync();
    }

    public async Task<AgentWorkflowDto> GetWorkflowByIdAsync(Guid id)
    {
        var workflow = await _context.AgentWorkflows
            .Include(w => w.Request)
            .Include(w => w.Steps)
                .ThenInclude(s => s.ToolCalls)
            .Include(w => w.ApprovalActions)
                .ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null) throw new NotFoundException($"Agent workflow '{id}' was not found.");

        return MapWorkflowToDto(workflow);
    }

    private static AgentWorkflowDto MapWorkflowToDto(AgentWorkflow w)
    {
        return new AgentWorkflowDto
        {
            Id = w.Id,
            RequestId = w.RequestId,
            RequestTitle = w.Request?.Title ?? string.Empty,
            WorkflowType = w.WorkflowType,
            Status = w.Status.ToString(),
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt,
            Steps = w.Steps.Select(s => new AgentStepDto
            {
                Id = s.Id,
                AgentName = s.AgentName,
                StepName = s.StepName,
                Status = s.Status.ToString(),
                InputDataJson = s.InputDataJson,
                OutputDataJson = s.OutputDataJson,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                ToolCalls = s.ToolCalls.Select(tc => new AgentToolCallDto
                {
                    Id = tc.Id,
                    ToolName = tc.ToolName,
                    InputJson = tc.InputJson,
                    OutputJson = tc.OutputJson,
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    Success = tc.Success
                }).ToList()
            }).ToList(),
            Approvals = w.ApprovalActions.Select(a => new ApprovalActionDto
            {
                Id = a.Id,
                WorkflowId = a.WorkflowId,
                Status = a.Status.ToString(),
                ReasonRequired = a.ReasonRequired,
                Comments = a.Comments,
                RequestedAt = a.RequestedAt,
                DecidedAt = a.DecidedAt,
                ApproverName = a.Approver != null ? $"{a.Approver.FirstName} {a.Approver.LastName}" : null
            }).ToList()
        };
    }
}

public class ApprovalService : IApprovalService
{
    private readonly FixFlowDbContext _context;

    public ApprovalService(FixFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<ApprovalActionDto>> GetPendingApprovalsAsync()
    {
        return await _context.ApprovalActions
            .Where(a => a.Status == ApprovalStatus.Pending)
            .OrderByDescending(a => a.RequestedAt)
            .Select(a => new ApprovalActionDto
            {
                Id = a.Id,
                WorkflowId = a.WorkflowId,
                Status = a.Status.ToString(),
                ReasonRequired = a.ReasonRequired,
                Comments = a.Comments,
                RequestedAt = a.RequestedAt
            })
            .ToListAsync();
    }

    public async Task<ApprovalActionDto> ProcessApprovalAsync(Guid approvalId, ApprovalDecisionDto decision, Guid approverId)
    {
        var approval = await _context.ApprovalActions
            .Include(a => a.Workflow)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        if (approval == null) throw new NotFoundException($"Approval action '{approvalId}' not found.");

        approval.Status = decision.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        approval.Comments = decision.Comments;
        approval.ApproverId = approverId;
        approval.DecidedAt = DateTime.UtcNow;

        if (approval.Workflow != null)
        {
            approval.Workflow.Status = decision.Approved ? WorkflowStatus.Approved : WorkflowStatus.Rejected;
        }

        await _context.SaveChangesAsync();

        return new ApprovalActionDto
        {
            Id = approval.Id,
            WorkflowId = approval.WorkflowId,
            Status = approval.Status.ToString(),
            ReasonRequired = approval.ReasonRequired,
            Comments = approval.Comments,
            RequestedAt = approval.RequestedAt,
            DecidedAt = approval.DecidedAt
        };
    }
}
