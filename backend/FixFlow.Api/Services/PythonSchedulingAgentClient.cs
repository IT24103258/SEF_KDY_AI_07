using System.Diagnostics;
using System.Text.Json;
using FixFlow.Api.DTOs;
using Microsoft.Extensions.Logging;

namespace FixFlow.Api.Services;

public class PythonAgentToolCallDto
{
    public string ToolName { get; set; } = string.Empty;
    public JsonElement? InputParams { get; set; }
    public JsonElement? OutputParams { get; set; }
    public int ExecutionTimeMs { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public interface IPythonSchedulingAgentClient
{
    Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(ScheduleRequestDto request, CancellationToken ct = default);
    Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(SchedulingAgentInvocation input, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

public class SchedulingAgentInvocation
{
    public Guid RequestId { get; set; }
    public Guid TechnicianId { get; set; }
    public string Priority { get; set; } = "Medium";
    public int EstimatedDurationMinutes { get; set; } = 60;
    public DateTime? PreferredStartTime { get; set; }
    public DateTime? PreferredEndTime { get; set; }
    public DateTime? SlaDeadline { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool? IsTechnicianAvailable { get; set; }
    public JsonElement? TechnicianCalendar { get; set; }
    public JsonElement? BusinessHours { get; set; }
    public JsonElement? ExistingBookings { get; set; }
    public bool SimulateConflict { get; set; }
    public bool WithinBusinessHours { get; set; } = true;
    public bool SlaCompliant { get; set; } = true;
}

public class PythonSchedulingResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public JsonElement? OutputData { get; set; }
    public bool ValidationPassed { get; set; }
    public int ToolCallCount { get; set; }
    public List<PythonAgentToolCallDto> ToolCalls { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public double LatencyMs { get; set; }
    public string ExecutionMode { get; set; } = "unknown";
    public bool? LlmUsed { get; set; }
    public string? LlmInterpretationSummary { get; set; }
    public string? SchedulingPath { get; set; }
}

public class PythonSchedulingAgentClient : IPythonSchedulingAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonSchedulingAgentClient> _logger;
    private readonly string _baseUrl;

    public PythonSchedulingAgentClient(HttpClient httpClient, IConfiguration configuration, ILogger<PythonSchedulingAgentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["AgenticAI:BaseUrl"] ?? "http://localhost:8000";
        var timeoutSeconds = configuration.GetValue<int>("AgenticAI:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(ScheduleRequestDto request, CancellationToken ct = default)
    {
        var invocation = new SchedulingAgentInvocation
        {
            RequestId = request.RequestId,
            TechnicianId = request.TechnicianId,
            Priority = request.Priority,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            PreferredStartTime = request.PreferredStartTime,
            PreferredEndTime = request.PreferredStartTime?.AddMinutes(request.EstimatedDurationMinutes > 0 ? request.EstimatedDurationMinutes : 60),
            SlaDeadline = request.SlaDeadline,
            Description = request.RequesterNotes
        };
        return ExecuteSchedulingAgentAsync(invocation, ct);
    }

    public async Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(SchedulingAgentInvocation input, CancellationToken ct = default)
    {
        var duration = input.EstimatedDurationMinutes > 0 ? input.EstimatedDurationMinutes : 60;

        var payload = new Dictionary<string, object>
        {
            ["request_id"] = input.RequestId.ToString(),
            ["workflow_type"] = "Scheduling",
            ["input_context"] = new Dictionary<string, object?>
            {
                ["request_id"] = input.RequestId.ToString(),
                ["assigned_technician_id"] = input.TechnicianId.ToString(),
                ["priority"] = input.Priority,
                ["estimated_duration_minutes"] = duration,
                ["preferred_start_time"] = input.PreferredStartTime?.ToString("o"),
                ["preferred_end_time"] = input.PreferredEndTime?.ToString("o") ?? input.PreferredStartTime?.AddMinutes(duration).ToString("o"),
                ["proposed_start_time"] = input.PreferredStartTime?.ToString("o"),
                ["proposed_end_time"] = input.PreferredEndTime?.ToString("o") ?? input.PreferredStartTime?.AddMinutes(duration).ToString("o"),
                ["sla_deadline"] = input.SlaDeadline?.ToString("o"),
                ["description"] = input.Description,
                ["is_technician_available"] = input.IsTechnicianAvailable,
                ["technician_calendar"] = input.TechnicianCalendar,
                ["business_hours"] = input.BusinessHours,
                ["existing_bookings"] = input.ExistingBookings,
                ["within_business_hours"] = input.WithinBusinessHours,
                ["sla_compliant"] = input.SlaCompliant,
                ["simulate_conflict"] = input.SimulateConflict,
            }
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/orchestrator/execute", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Python orchestrator returned {StatusCode}", response.StatusCode);
                return new PythonSchedulingResult
                {
                    Success = false,
                    ErrorMessage = $"Python orchestrator returned HTTP {(int)response.StatusCode}",
                    Status = "PYTHON_ERROR",
                    LatencyMs = sw.Elapsed.TotalMilliseconds
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var steps = root.GetProperty("steps");
            JsonElement? schedulingStep = null;
            foreach (var step in steps.EnumerateArray())
            {
                if (step.TryGetProperty("step_name", out var stepName) &&
                    stepName.GetString()?.Contains("Scheduling", StringComparison.OrdinalIgnoreCase) == true)
                {
                    schedulingStep = step;
                    break;
                }
            }

            if (schedulingStep == null)
            {
                return new PythonSchedulingResult
                {
                    Success = false,
                    ErrorMessage = "No scheduling step found in orchestrator response",
                    Status = "NO_SCHEDULING_STEP",
                    LatencyMs = sw.Elapsed.TotalMilliseconds
                };
            }

            var stepStatus = schedulingStep.Value.GetProperty("status").GetString();
            var outputData = schedulingStep.Value.GetProperty("output_data");
            var validationPassed = schedulingStep.Value.GetProperty("validation_passed").GetBoolean();

            var toolCalls = new List<PythonAgentToolCallDto>();
            if (schedulingStep.Value.TryGetProperty("tool_calls", out var toolCallsElement))
            {
                foreach (var tc in toolCallsElement.EnumerateArray())
                {
                    var toolCall = new PythonAgentToolCallDto
                    {
                        ToolName = tc.TryGetProperty("tool_name", out var tn) ? tn.GetString() ?? "" : "",
                        InputParams = tc.TryGetProperty("input_params", out var ip) ? ip : null,
                        OutputParams = tc.TryGetProperty("output_params", out var op) ? op : null,
                        ExecutionTimeMs = tc.TryGetProperty("execution_time_ms", out var et) ? et.GetInt32() : 0,
                        Success = !tc.TryGetProperty("success", out var s) || s.GetBoolean(),
                        ErrorMessage = tc.TryGetProperty("error_message", out var em) ? em.GetString() : null
                    };
                    toolCalls.Add(toolCall);
                }
            }

            string executionMode = "deterministic_fallback";
            bool? llmUsed = null;
            string? llmSummary = null;
            string? schedulingPath = null;
            if (outputData.ValueKind == JsonValueKind.Object)
            {
                if (outputData.TryGetProperty("llm_used", out var llmEl) && llmEl.ValueKind == JsonValueKind.True)
                    llmUsed = true;
                else if (outputData.TryGetProperty("llm_used", out var llmEl2) && llmEl2.ValueKind == JsonValueKind.False)
                    llmUsed = false;

                if (outputData.TryGetProperty("scheduling_path", out var spEl))
                    schedulingPath = spEl.GetString();

                if (outputData.TryGetProperty("llm_interpretation_summary", out var lsEl))
                    llmSummary = lsEl.GetString();

                executionMode = (llmUsed == true) ? "llm_assisted" : "deterministic_fallback";
            }

            sw.Stop();
            return new PythonSchedulingResult
            {
                Success = stepStatus == "SUCCESS" || stepStatus == "REQUIRES_HUMAN_APPROVAL",
                Status = stepStatus,
                OutputData = outputData,
                ValidationPassed = validationPassed,
                ToolCallCount = toolCalls.Count,
                ToolCalls = toolCalls,
                LatencyMs = sw.Elapsed.TotalMilliseconds,
                ExecutionMode = executionMode,
                LlmUsed = llmUsed,
                LlmInterpretationSummary = llmSummary,
                SchedulingPath = schedulingPath
            };
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Python orchestrator request timed out");
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = "Python orchestrator request timed out",
                Status = "TIMEOUT",
                LatencyMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Python orchestrator unreachable at {BaseUrl}", _baseUrl);
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = $"Python orchestrator unreachable: {ex.Message}",
                Status = "UNREACHABLE",
                LatencyMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected error calling Python orchestrator");
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Status = "ERROR",
                LatencyMs = sw.Elapsed.TotalMilliseconds
            };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
