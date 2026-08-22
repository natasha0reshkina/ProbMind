using System.Diagnostics;
using ProbMind.Api.Operations;

namespace ProbMind.Api.Middleware;

public sealed class ApiMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public ApiMetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApiMetricsRegistry registry)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var endpoint = context.GetEndpoint();
            var route = endpoint?.DisplayName;
            if (string.IsNullOrWhiteSpace(route))
                route = context.Request.Path.Value ?? "/";
            registry.Record(
                context.Request.Method,
                route,
                stopwatch.Elapsed,
                context.Response.StatusCode,
                DateTimeOffset.UtcNow);
        }
    }
}
