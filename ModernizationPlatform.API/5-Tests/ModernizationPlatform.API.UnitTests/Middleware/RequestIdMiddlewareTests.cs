using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using ModernizationPlatform.API.Middleware;

namespace ModernizationPlatform.API.UnitTests.Middleware;

public sealed class RequestIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesProvidedHeaderAndAddsResponseHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "req-456";
        using var activity = new Activity("test-activity");
        activity.Start();

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal("req-456", context.Items[RequestIdMiddleware.HeaderName]);
        Assert.Equal("req-456", context.Response.Headers[RequestIdMiddleware.HeaderName]);
        Assert.Contains(activity.Tags, tag => tag.Key == "requestId" && tag.Value == "req-456");

        activity.Stop();
    }
}
