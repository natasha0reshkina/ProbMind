namespace ProbMind.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            headers.TryAdd("Cross-Origin-Resource-Policy", "same-site");
            if (context.Request.IsHttps)
                headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
