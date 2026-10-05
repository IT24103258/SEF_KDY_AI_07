using System.Text.Json;
using FixFlow.Api.DTOs;
using Microsoft.Extensions.Logging;

namespace FixFlow.Api.Services;

public interface IPythonSchedulingAgentClient
{
    Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(ScheduleRequestDto request, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

public class PythonSchedulingResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public JsonElement? OutputData { get; set; }
    public bool ValidationPassed { get; set; }
    public int ToolCallCount { get; set; }
    public string? ErrorMessage { get; set; }
    public double LatencyMs { get; set; }
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

    public async Task<PythonSchedulingResult?> ExecuteSchedulingAgentAsync(ScheduleRequestDto request, CancellationToken ct = default)
    {
        var payload = new
        {
            input_context = new
            {
                request_id = request.RequestId.ToString(),
                assigned_technician_id = request.TechnicianId.ToString(),
                priority = request.Priority,
                estimated_duration_minutes = request.EstimatedDurationMinutes,
                preferred_start_time = request.PreferredStartTime?.ToString("o"),
                preferred_end_time = request.PreferredStartTime?.AddMinutes(request.EstimatedDurationMinutes > 0 ? request.EstimatedDurationMinutes : 60).ToString("o"),
                sla_deadline = request.SlaDeadline?.ToString("o"),
            }
        };

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
                    Status = "PYTHON_ERROR"
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
                    Status = "NO_SCHEDULING_STEP"
                };
            }

            var stepStatus = schedulingStep.Value.GetProperty("status").GetString();
            var outputData = schedulingStep.Value.GetProperty("output_data");
            var validationPassed = schedulingStep.Value.GetProperty("validation_passed").GetBoolean();

            int toolCallCount = 0;
            if (schedulingStep.Value.TryGetProperty("tool_calls", out var toolCalls))
            {
                toolCallCount = toolCalls.GetArrayLength();
            }

            return new PythonSchedulingResult
            {
                Success = stepStatus == "SUCCESS",
                Status = stepStatus,
                OutputData = outputData,
                ValidationPassed = validationPassed,
                ToolCallCount = toolCallCount
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Python orchestrator request timed out");
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = "Python orchestrator request timed out",
                Status = "TIMEOUT"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Python orchestrator unreachable at {BaseUrl}", _baseUrl);
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = $"Python orchestrator unreachable: {ex.Message}",
                Status = "UNREACHABLE"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Python orchestrator");
            return new PythonSchedulingResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Status = "ERROR"
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
