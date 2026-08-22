using System.Net;
using System.Text.Json;

namespace ProbMind.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            var (status, code) = exception switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "not_found"),
                ArgumentException => (HttpStatusCode.BadRequest, "validation_error"),
                InvalidOperationException => (HttpStatusCode.Conflict, "invalid_operation"),
                _ => (HttpStatusCode.InternalServerError, "internal_error")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning(exception, "Handled request failure {Code} for {Path}", code, context.Request.Path);

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                error = code,
                message = status == HttpStatusCode.InternalServerError
                    ? "Unexpected server error."
                    : exception.Message,
                requestId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
