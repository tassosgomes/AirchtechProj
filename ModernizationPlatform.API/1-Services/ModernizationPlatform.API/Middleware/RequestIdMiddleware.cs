using System.Diagnostics;
using System.Linq;
using Sentry;
using Serilog.Context;

namespace ModernizationPlatform.API.Middleware;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-Id";

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);

        context.Items[HeaderName] = requestId;
        Activity.Current?.SetTag("requestId", requestId);
        SentrySdk.ConfigureScope(scope => scope.SetTag("requestId", requestId));

        using (LogContext.PushProperty("requestId", requestId))
        {
            context.Response.Headers[HeaderName] = requestId;
            if (!context.Response.Headers.ContainsKey("Access-Control-Expose-Headers"))
            {
                context.Response.Headers.Append("Access-Control-Expose-Headers", HeaderName);
            }

            await _next(context);
        }
    }

    private static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            var headerValue = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        return Guid.NewGuid().ToString();
    }
}
