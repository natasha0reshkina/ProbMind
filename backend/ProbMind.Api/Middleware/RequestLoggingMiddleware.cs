using System.Diagnostics;

namespace ProbMind.Api.Middleware;

public sealed class RequestLoggingMiddleware
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
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.Request.Headers.TryGetValue("X-Request-Id", out var supplied)
            ? supplied.ToString()
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = requestId;
        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path} => {StatusCode} in {ElapsedMs}ms request_id={RequestId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                requestId);
        }
    }
}
