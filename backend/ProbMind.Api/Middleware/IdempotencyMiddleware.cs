using System.Text;
using ProbMind.Application.Abstractions.Cache;

namespace ProbMind.Api.Middleware;

public sealed record CachedHttpResponse(int StatusCode, string ContentType, string Body, Dictionary<string, string> Headers);

public sealed class IdempotencyMiddleware
{
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationCache cache)
    {
        if (!ProtectedMethods.Contains(context.Request.Method) ||
            !context.Request.Headers.TryGetValue("X-Idempotency-Key", out var header) ||
            string.IsNullOrWhiteSpace(header.ToString()))
        {
            await _next(context);
            return;
        }

        var rawKey = header.ToString().Trim();
        if (rawKey.Length is < 8 or > 128)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "invalid_idempotency_key",
                message = "X-Idempotency-Key must contain between 8 and 128 characters.",
                requestId = context.TraceIdentifier
            });
            return;
        }

        var userIdentity = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "authenticated"
            : "anonymous";
        var cacheKey = $"idempotency:{userIdentity}:{context.Request.Method}:{context.Request.Path}:{rawKey}";
        var existing = await cache.GetAsync<CachedHttpResponse>(cacheKey, context.RequestAborted);
        if (existing is not null)
        {
            await ReplayAsync(context, existing);
            context.Response.Headers["X-Idempotency-Replayed"] = "true";
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);
            buffer.Position = 0;
            var body = await new StreamReader(buffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(context.RequestAborted);
            buffer.Position = 0;

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var headers = context.Response.Headers
                    .Where(x => !string.Equals(x.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                var cached = new CachedHttpResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    body,
                    headers);
                await cache.SetAsync(cacheKey, cached, TimeSpan.FromMinutes(15), context.RequestAborted);
            }

            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Idempotent request failed and was not cached. key={CacheKey}", cacheKey);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static async Task ReplayAsync(HttpContext context, CachedHttpResponse response)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        foreach (var (name, value) in response.Headers)
        {
            if (!string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                context.Response.Headers[name] = value;
        }
        await context.Response.WriteAsync(response.Body, context.RequestAborted);
    }
}
