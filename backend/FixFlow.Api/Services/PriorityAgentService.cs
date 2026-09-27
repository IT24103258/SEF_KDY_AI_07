using System.Net.Http.Json;
using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
using FixFlow.Api.Models;
using FixFlow.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Services;

public class PriorityAgentService : IPriorityAgentService
{
    private readonly HttpClient _httpClient;
    private readonly FixFlowDbContext _context;
    private readonly IPriorityAssessmentService _priorityAssessmentService;
    private readonly ILogger<PriorityAgentService> _logger;

    public PriorityAgentService(
        HttpClient httpClient,
        FixFlowDbContext context,
        IPriorityAssessmentService priorityAssessmentService,
        ILogger<PriorityAgentService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _priorityAssessmentService = priorityAssessmentService;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Preview (no persistence)
    // -----------------------------------------------------------------------

    public async Task<PriorityAgentResultDto> EvaluateAgentAsync(Guid requestId, PriorityAgentEvaluationRequestDto? overrideDto = null)
    {
        var (request, executionResult) = await RunWorkflowAsync(requestId, overrideDto);
        return ValidateAndExtract(requestId, executionResult);
    }

    // -----------------------------------------------------------------------
    // Evaluate + persist assessment AND auditable workflow state
    // -----------------------------------------------------------------------

    public async Task<PriorityAssessmentDto> EvaluateAndPersistAsync(
        Guid requestId,
        PriorityAgentEvaluationRequestDto? overrideDto = null,
        string assessedBy = "PriorityAgent")
    {
        var (request, executionResult) = await RunWorkflowAsync(requestId, overrideDto);

        PriorityAgentResultDto? validated = null;
        Exception? failure = null;
        try
        {
            validated = ValidateAndExtract(requestId, executionResult);
        }
        catch (Exception ex)
        {
            // Safe failure: capture the reason, persist the failed workflow state for
            // auditability, then surface the failure. We never invent an assessment.
            failure = ex;
        }

        await PersistWorkflowStateAsync(request, executionResult, validated, failure, assessedBy);

        if (failure != null)
        {
            throw failure;
        }

        return await _priorityAssessmentService.SaveAgentAssessmentAsync(requestId, validated!, assessedBy);
    }

    // -----------------------------------------------------------------------
    // Orchestration call
    // -----------------------------------------------------------------------

    private async Task<(MaintenanceRequest Request, PriorityAgentWorkflowExecutionResult Result)> RunWorkflowAsync(
        Guid requestId, PriorityAgentEvaluationRequestDto? overrideDto)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Asset)
            .Include(r => r.Location)
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted);

        if (request == null)
        {
            throw new NotFoundException($"Maintenance request with ID '{requestId}' was not found.");
        }

        var inputContext = await BuildInputContextAsync(request, overrideDto);

        var payload = new PriorityAgentWorkflowExecutionRequest
        {
            RequestId = request.Id.ToString(),
            WorkflowType = "Priority",
            InputContext = inputContext
        };

        _logger.LogInformation("Invoking Python Agentic AI for request {RequestId} via {BaseAddress}api/orchestrator/execute",
            requestId, _httpClient.BaseAddress);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/orchestrator/execute", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Python Agentic AI microservice at {BaseAddress}", _httpClient.BaseAddress);
            throw new InvalidOperationException($"Unable to connect to Python Agentic AI microservice: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Python Agentic AI responded with status {StatusCode}: {Error}", response.StatusCode, errContent);
            throw new InvalidOperationException($"Python Agentic AI execution failed with status {response.StatusCode}: {errContent}");
        }

        var executionResult = await response.Content.ReadFromJsonAsync<PriorityAgentWorkflowExecutionResult>();
        if (executionResult == null || executionResult.Steps == null)
        {
            throw new InvalidOperationException("Invalid empty response received from Python Agentic AI service.");
        }

        return (request, executionResult);
    }

    /// <summary>
    /// Builds the authoritative input context for the Python PriorityAgent.
    /// Every signal that the C# deterministic engine (CreatePriorityAssessmentAsync)
    /// uses is derived here with the SAME rules and passed explicitly, so the Python
    /// agent never relies on mock tool defaults and both engines agree.
    /// </summary>
    private async Task<Dictionary<string, object?>> BuildInputContextAsync(
        MaintenanceRequest request, PriorityAgentEvaluationRequestDto? overrideDto)
    {
        bool hasSafetyHazard = overrideDto?.HasSafetyHazard ?? PriorityAssessmentService.DetectHazard(
            request.Title,
            request.Description,
            overrideDto?.HazardDetails,
            overrideDto?.DisruptionInformation);

        // Authoritative asset criticality input (override wins, else the stored asset value).
        // Mirrors C# AssessAssetCriticality(override ?? asset.Criticality, category).
        string effectiveCriticality = overrideDto?.AssetCriticalityOverride ?? request.Asset?.Criticality ?? string.Empty;
        string effectiveCategory = overrideDto?.AssetCategory
            ?? request.Asset?.Category
            ?? request.Category?.Name
            ?? string.Empty;

        // Authoritative open-request count for the asset (same query as the deterministic engine).
        int openRequests = 0;
        if (request.AssetId.HasValue)
        {
            openRequests = await _context.MaintenanceRequests
                .CountAsync(r => r.AssetId == request.AssetId && r.Id != request.Id
                    && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Cancelled);
        }

        // Authoritative high-density determination (same rule as the deterministic engine).
        bool isHighDensity = overrideDto?.HighDensityLocation
            ?? (request.Location?.Building == "Common Areas"
                || (request.Location?.Room != null && request.Location.Room.Contains("Lobby", StringComparison.OrdinalIgnoreCase)));

        int recentFailures = overrideDto?.RecentFailureCount ?? 0;

        var inputContext = new Dictionary<string, object?>
        {
            ["request_id"] = request.Id.ToString(),
            ["title"] = request.Title,
            ["description"] = request.Description,
            ["asset_id"] = request.AssetId?.ToString() ?? "",
            ["location_id"] = request.LocationId.ToString(),
            ["location"] = request.Location != null ? $"{request.Location.Building} - {request.Location.Room}".Trim(' ', '-') : (overrideDto?.LocationInfo ?? ""),
            ["category"] = effectiveCategory,
            ["asset_category"] = effectiveCategory,
            ["asset_criticality"] = effectiveCriticality,
            ["disruption_information"] = overrideDto?.DisruptionInformation ?? "",
            ["disruption_scope"] = overrideDto?.DisruptionScope ?? "",
            ["failure_history"] = overrideDto?.FailureHistory ?? "",
            ["recent_failures"] = recentFailures,
            ["open_request_count"] = openRequests,
            ["is_high_density"] = isHighDensity,
            ["has_safety_hazard"] = hasSafetyHazard,
            ["hazard_flag"] = hasSafetyHazard
        };

        if (!string.IsNullOrWhiteSpace(overrideDto?.AssetCriticalityOverride))
        {
            inputContext["asset_criticality_override"] = overrideDto.AssetCriticalityOverride;
        }
        if (!string.IsNullOrWhiteSpace(overrideDto?.ImpactOverride))
        {
            inputContext["impact_override"] = overrideDto.ImpactOverride;
            inputContext["impact"] = overrideDto.ImpactOverride;
        }
        if (!string.IsNullOrWhiteSpace(overrideDto?.LikelihoodOverride))
        {
            inputContext["likelihood_override"] = overrideDto.LikelihoodOverride;
            inputContext["likelihood"] = overrideDto.LikelihoodOverride;
        }

        return inputContext;
    }

    // -----------------------------------------------------------------------
    // Deterministic validation of the agent workflow result
    // -----------------------------------------------------------------------

    private PriorityAgentResultDto ValidateAndExtract(Guid requestId, PriorityAgentWorkflowExecutionResult executionResult)
    {
        // 1. Confirm overall workflow is not FAILED
        if (string.Equals(executionResult.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Agentic AI workflow execution failed for request {RequestId}: {Error}", requestId, executionResult.Error);
            throw new InvalidOperationException($"Agentic AI workflow returned FAILED status: {executionResult.Error ?? "Workflow execution failed requiring manual review."}");
        }

        var priorityStep = executionResult.Steps.FirstOrDefault(s =>
            string.Equals(s.AgentName, "PriorityAgent", StringComparison.OrdinalIgnoreCase) ||
            s.StepName.Contains("Priority", StringComparison.OrdinalIgnoreCase));

        if (priorityStep == null || priorityStep.OutputData == null)
        {
            throw new InvalidOperationException("PriorityAgent step output not found in workflow execution results.");
        }

        // 2. Confirm step status is not FAILED (safe-failure / tool failure must not be persisted as normal)
        if (string.Equals(priorityStep.Status, "FAILED", StringComparison.OrdinalIgnoreCase))
        {
            var err = priorityStep.OutputData.Error ?? priorityStep.OutputData.Explanation ?? "Agent step failed during evaluation";
            _logger.LogWarning("PriorityAgent step failed for request {RequestId}: {Reason}. Requires manual human review.", requestId, err);
            throw new InvalidOperationException($"PriorityAgent step failed and requires manual human review: {err}");
        }

        // 3. Confirm deterministic validation passed
        if (priorityStep.ValidationPassed != true)
        {
            _logger.LogWarning("PriorityAgent deterministic validation failed for request {RequestId}", requestId);
            throw new InvalidOperationException("PriorityAgent deterministic validation failed. Assessment cannot be trusted for persistence.");
        }

        var outData = priorityStep.OutputData;

        // 4. Confirm required structured output fields are valid
        if (outData.RiskScore < 1 || outData.RiskScore > 100)
        {
            throw new InvalidOperationException($"Invalid agent RiskScore {outData.RiskScore}. Must be between 1 and 100.");
        }

        if (string.IsNullOrWhiteSpace(outData.Priority) || string.IsNullOrWhiteSpace(outData.RiskLevel))
        {
            throw new InvalidOperationException("PriorityAgent returned incomplete structured assessment (Priority or RiskLevel missing).");
        }

        // 5. Confirm risk level is internally consistent with the score
        string expectedRiskLevel = _priorityAssessmentService.DetermineRiskLevel(outData.RiskScore);
        if (!string.Equals(expectedRiskLevel, outData.RiskLevel, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"PriorityAgent inconsistent assessment: RiskScore {outData.RiskScore} maps to '{expectedRiskLevel}', but agent reported '{outData.RiskLevel}'.");
        }

        // 6. Cross-engine verification: when the agent supplies its contributing factors,
        //    independently recompute the score with the authoritative C# formula and refuse
        //    to persist any assessment where the two engines disagree.
        if (outData.ContributingFactors != null)
        {
            var cf = outData.ContributingFactors;
            bool hazard = cf.HasSafetyHazard || outData.HazardFlag == true || outData.HazardDetected == true;
            bool isHighDensity = cf.LocationModifier >= 5;

            var (expectedScore, _) = _priorityAssessmentService.CalculateRiskScore(
                outData.AssetCriticality,
                outData.ImpactLevel,
                outData.LikelihoodLevel,
                hazard,
                cf.RecentFailureCount,
                isHighDensity);

            if (expectedScore != outData.RiskScore)
            {
                _logger.LogWarning(
                    "PriorityAgent cross-engine mismatch for request {RequestId}: agent={AgentScore}, deterministic={ExpectedScore}",
                    requestId, outData.RiskScore, expectedScore);
                throw new InvalidOperationException(
                    $"PriorityAgent score mismatch: agent reported {outData.RiskScore} but deterministic recalculation " +
                    $"from its contributing factors yields {expectedScore}. Refusing to persist an inconsistent assessment.");
            }
        }

        _logger.LogInformation("PriorityAgent evaluation succeeded for request {RequestId}: Score={Score}, Priority={Priority}",
            requestId, outData.RiskScore, outData.Priority);

        return outData;
    }

    // -----------------------------------------------------------------------
    // Workflow-state persistence (auditable; records success AND safe failure)
    // -----------------------------------------------------------------------

    private async Task PersistWorkflowStateAsync(
        MaintenanceRequest request,
        PriorityAgentWorkflowExecutionResult executionResult,
        PriorityAgentResultDto? validated,
        Exception? failure,
        string assessedBy)
    {
        bool failed = failure != null
            || string.Equals(executionResult.Status, "FAILED", StringComparison.OrdinalIgnoreCase);

        var workflow = new AgentWorkflow
        {
            RequestId = request.Id,
            WorkflowType = string.IsNullOrWhiteSpace(executionResult.WorkflowType) ? "Priority" : executionResult.WorkflowType!,
            Status = failed ? WorkflowStatus.Failed : WorkflowStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            OutputSummaryJson = JsonSerializer.Serialize(new
            {
                objective = executionResult.Objective,
                plan = executionResult.Plan,
                status = executionResult.Status,
                requires_human_approval = executionResult.RequiresHumanApproval || failed,
                approval_reason = failure?.Message ?? executionResult.ApprovalReason,
                risk_score = validated?.RiskScore,
                risk_level = validated?.RiskLevel,
                priority = validated?.Priority,
                error = failure?.Message
            })
        };

        foreach (var stepResult in executionResult.Steps)
        {
            var step = new AgentStep
            {
                AgentName = stepResult.AgentName,
                StepName = stepResult.StepName,
                Status = MapStepStatus(stepResult.Status, failed && stepResult.AgentName == "PriorityAgent"),
                InputDataJson = JsonSerializer.Serialize(new { request_id = request.Id.ToString() }),
                OutputDataJson = stepResult.OutputData != null ? JsonSerializer.Serialize(stepResult.OutputData) : string.Empty,
                ValidationResultJson = JsonSerializer.Serialize(new
                {
                    validation_passed = stepResult.ValidationPassed,
                    status = stepResult.Status
                }),
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            foreach (var tc in stepResult.ToolCalls)
            {
                step.ToolCalls.Add(new AgentToolCall
                {
                    ToolName = tc.ToolName,
                    InputJson = tc.InputParams.HasValue ? tc.InputParams.Value.GetRawText() : string.Empty,
                    OutputJson = tc.OutputParams.HasValue ? tc.OutputParams.Value.GetRawText() : string.Empty,
                    ExecutionTimeMs = tc.ExecutionTimeMs,
                    Success = tc.Success,
                    ErrorMessage = tc.ErrorMessage
                });
            }

            workflow.Steps.Add(step);
        }

        await _context.AgentWorkflows.AddAsync(workflow);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = failed ? "PriorityAgentWorkflowFailed" : "PriorityAgentWorkflowCompleted",
            EntityName = "AgentWorkflow",
            EntityId = workflow.Id.ToString(),
            ChangesJson = JsonSerializer.Serialize(new
            {
                RequestId = request.Id,
                WorkflowId = workflow.Id,
                Status = workflow.Status.ToString(),
                RequiresHumanReview = executionResult.RequiresHumanApproval || failed,
                AssessedBy = assessedBy,
                Error = failure?.Message
            }),
            IpAddress = "PriorityAgent/Service"
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Persisted PriorityAgent workflow {WorkflowId} for request {RequestId} with status {Status}",
            workflow.Id, request.Id, workflow.Status);
    }

    private static WorkflowStatus MapStepStatus(string status, bool forceFailed)
    {
        if (forceFailed) return WorkflowStatus.Failed;
        return status?.ToUpperInvariant() switch
        {
            "SUCCESS" => WorkflowStatus.Completed,
            "FAILED" => WorkflowStatus.Failed,
            "REQUIRES_HUMAN_APPROVAL" => WorkflowStatus.WaitingForApproval,
            _ => WorkflowStatus.Pending
        };
    }
}
