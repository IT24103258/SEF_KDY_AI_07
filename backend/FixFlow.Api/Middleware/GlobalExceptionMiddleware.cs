using System.Net;
using System.Text.Json;
using FixFlow.Api.DTOs;
using FixFlow.Api.Exceptions;

namespace FixFlow.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            NotFoundException nf => (HttpStatusCode.NotFound, nf.Message, (List<string>?)null),
            ValidationException ve => (HttpStatusCode.BadRequest, ve.Message, ve.Errors),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, ue.Message, (List<string>?)null),
            ForbiddenException fe => (HttpStatusCode.Forbidden, fe.Message, (List<string>?)null),
            ConflictException ce => (HttpStatusCode.Conflict, ce.Message, (List<string>?)null),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred. Please try again later.", (List<string>?)null)
        };

        context.Response.StatusCode = (int)statusCode;
        var response = ApiResponse<object>.FailureResult(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return context.Response.WriteAsync(json);
    }
}
