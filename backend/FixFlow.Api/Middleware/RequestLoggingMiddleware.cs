using System.Text.RegularExpressions;

namespace FixFlow.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var method = request.Method;
        var path = request.Path;
        var queryString = request.QueryString.HasValue ? ScrubQueryString(request.QueryString.Value!) : string.Empty;

        _logger.LogInformation("HTTP Request: {Method} {Path}{QueryString}", method, path, queryString);

        await _next(context);

        _logger.LogInformation("HTTP Response: {Method} {Path} -> {StatusCode}", method, path, context.Response.StatusCode);
    }

    private static string ScrubQueryString(string queryString)
    {
        // Redact any query params containing password, token, or secret
        return Regex.Replace(queryString, @"(token|password|secret|key)=([^&]*)", "$1=[REDACTED]", RegexOptions.IgnoreCase);
    }
}
