using System.IO;
using System.Text.Json;
using ModernizationPlatform.API.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace ModernizationPlatform.API.UnitTests.Observability;

public sealed class StructuredJsonFormatterTests
{
    [Fact]
    public void Format_IncludesRequiredFields()
    {
        var formatter = new StructuredJsonFormatter("modernization-api", "1.0.0");
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Mensagem", new[] { new TextToken("Mensagem") }),
            new[]
            {
                new LogEventProperty("requestId", new ScalarValue("req-123"))
            });

        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);

        using var document = JsonDocument.Parse(writer.ToString());
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.Equal("Information", root.GetProperty("level").GetString());
        Assert.Equal("Mensagem", root.GetProperty("message").GetString());
        Assert.Equal("modernization-api", root.GetProperty("service.name").GetString());
        Assert.True(root.TryGetProperty("trace_id", out _));
        Assert.Equal("req-123", root.GetProperty("requestId").GetString());
    }
}
