using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace ModernizationPlatform.Worker.Logging;

public sealed class StructuredJsonFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _serviceName;
    private readonly string _serviceVersion;

    public StructuredJsonFormatter(string serviceName, string serviceVersion)
    {
        _serviceName = serviceName;
        _serviceVersion = serviceVersion;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var activity = Activity.Current;
        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage(),
            ["service.name"] = _serviceName,
            ["service.version"] = _serviceVersion,
            ["service"] = new Dictionary<string, object?>
            {
                ["name"] = _serviceName,
                ["version"] = _serviceVersion
            },
            ["trace_id"] = activity?.TraceId.ToString(),
            ["span_id"] = activity?.SpanId.ToString(),
            ["trace"] = new Dictionary<string, object?>
            {
                ["trace_id"] = activity?.TraceId.ToString(),
                ["span_id"] = activity?.SpanId.ToString()
            }
        };

        foreach (var property in logEvent.Properties)
        {
            if (!payload.ContainsKey(property.Key))
            {
                payload[property.Key] = RenderValue(property.Value);
            }
        }

        if (!payload.ContainsKey("requestId"))
        {
            if (logEvent.Properties.TryGetValue("RequestId", out var requestIdValue))
            {
                payload["requestId"] = RenderValue(requestIdValue);
            }
            else if (logEvent.Properties.TryGetValue("request_id", out var snakeCaseRequestId))
            {
                payload["requestId"] = RenderValue(snakeCaseRequestId);
            }
        }

        if (logEvent.Exception != null)
        {
            payload["exception"] = logEvent.Exception.ToString();
        }

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        output.WriteLine(json);
    }

    private static object? RenderValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            SequenceValue sequence => sequence.Elements.Select(RenderValue).ToArray(),
            StructureValue structure => structure.Properties.ToDictionary(p => p.Name, p => RenderValue(p.Value)),
            DictionaryValue dictionary => dictionary.Elements.ToDictionary(
                element => RenderValue(element.Key)?.ToString() ?? string.Empty,
                element => RenderValue(element.Value)),
            _ => value.ToString()
        };
    }
}
