using System.Text;
using System.Text.Json;
using FixFlow.Api.DTOs;
using FixFlow.Api.Interfaces;

namespace FixFlow.Api.Services;

/// <summary>
/// Calls the Python FastAPI classification microservice over HTTP.
/// Mirrors the ExternalDistanceMatrixService pattern — all failures return null
/// so the caller (RequestService.ClassifyAsync) can apply the fallback logic.
///
/// Base URL is read from configuration key "ClassificationAgent:BaseUrl".
/// If not present, defaults to "http://localhost:8000" (the default uvicorn port).
/// </summary>
public class ClassificationAgentService : IClassificationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClassificationAgentService> _logger;
    private readonly string _baseUrl;

    // The Python endpoint we POST to.  Built in Step 3 as POST /api/classify.
    private const string ClassifyEndpoint = "/api/classify";

    public ClassificationAgentService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClassificationAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ClassificationAgent:BaseUrl"] ?? "http://localhost:8000";
    }

    /// <summary>
    /// Sends the request title and description to the Python agent.
    /// A 10-second CancellationToken is applied per the spec — if the agent doesn't
    /// respond in time we log a warning and return null (the service will fall back).
    ///
    /// SECURITY NOTE: title and description are serialized as JSON string values,
    /// so they can never override the system instructions on the Python side.
    /// </summary>
    public async Task<AgentClassificationResponseDto?> ClassifyAsync(
        string title,
        string description,
        CancellationToken ct = default)
    {
        try
        {
            // 10-second per-call timeout, merged with any caller-supplied token.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            // Serialize as a JSON object — values are data, never instructions.
            var payload = JsonSerializer.Serialize(
                new { title, description },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling classification agent at {Url}{Endpoint}", _baseUrl, ClassifyEndpoint);

            var response = await _httpClient.PostAsync($"{_baseUrl}{ClassifyEndpoint}", content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Classification agent returned non-success status {StatusCode}.",
                    response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            return JsonSerializer.Deserialize<AgentClassificationResponseDto>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Classification agent timed out after 10 seconds.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification agent HTTP call failed.");
            return null;
        }
    }
}
