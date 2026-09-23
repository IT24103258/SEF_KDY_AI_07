using System.Net.Http.Json;
using System.Text.Json;
using FixFlow.Api.Data;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;
using FixFlow.Api.Interfaces;
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

    public async Task<PriorityAgentResultDto> EvaluateAgentAsync(Guid requestId, PriorityAgentEvaluationRequestDto? overrideDto = null)
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

        bool hasSafetyHazard = overrideDto?.HasSafetyHazard ?? (
            request.Title.Contains("gas", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("spark", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("smoke", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("leak", StringComparison.OrdinalIgnoreCase) ||
            request.Title.Contains("hazard", StringComparison.OrdinalIgnoreCase));

        var inputContext = new Dictionary<string, object?>
        {
            ["request_id"] = request.Id.ToString(),
            ["title"] = request.Title,
            ["description"] = request.Description,
            ["asset_id"] = request.AssetId?.ToString() ?? "",
            ["location_id"] = request.LocationId.ToString(),
            ["category"] = request.Category?.Name ?? "HVAC",
            ["has_safety_hazard"] = hasSafetyHazard,
            ["hazard_flag"] = hasSafetyHazard
        };

        if (!string.IsNullOrWhiteSpace(overrideDto?.AssetCriticalityOverride))
        {
            inputContext["asset_criticality"] = overrideDto.AssetCriticalityOverride;
        }

        if (!string.IsNullOrWhiteSpace(overrideDto?.ImpactOverride))
        {
            inputContext["impact"] = overrideDto.ImpactOverride;
        }

        if (!string.IsNullOrWhiteSpace(overrideDto?.LikelihoodOverride))
        {
            inputContext["likelihood"] = overrideDto.LikelihoodOverride;
        }

        if (!string.IsNullOrWhiteSpace(overrideDto?.DisruptionScope))
        {
            inputContext["disruption_scope"] = overrideDto.DisruptionScope;
        }

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

        var priorityStep = executionResult.Steps.FirstOrDefault(s =>
            string.Equals(s.AgentName, "PriorityAgent", StringComparison.OrdinalIgnoreCase) ||
            s.StepName.Contains("Priority", StringComparison.OrdinalIgnoreCase));

        if (priorityStep == null || priorityStep.OutputData == null)
        {
            throw new InvalidOperationException("PriorityAgent step output not found in workflow execution results.");
        }

        _logger.LogInformation("PriorityAgent evaluation succeeded for request {RequestId}: Score={Score}, Priority={Priority}",
            requestId, priorityStep.OutputData.RiskScore, priorityStep.OutputData.Priority);

        return priorityStep.OutputData;
    }

    public async Task<PriorityAssessmentDto> EvaluateAndPersistAsync(
        Guid requestId,
        PriorityAgentEvaluationRequestDto? overrideDto = null,
        string assessedBy = "PriorityAgent")
    {
        var agentResult = await EvaluateAgentAsync(requestId, overrideDto);
        return await _priorityAssessmentService.SaveAgentAssessmentAsync(requestId, agentResult, assessedBy);
    }
}
