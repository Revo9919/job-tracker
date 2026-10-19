using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JobTracker.Api.Middleware;

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
        var startedAt = DateTime.UtcNow;

        await _next(context);

        var elapsedMilliseconds = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds:0} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMilliseconds);
    }
}
