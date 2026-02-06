using System.Text.Json;

namespace ModernizationPlatform.Infra.Messaging.Messaging;

public static class RabbitMqJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
    }

    public static T? Deserialize<T>(ReadOnlyMemory<byte> body)
    {
        return JsonSerializer.Deserialize<T>(body.Span, JsonOptions);
    }
}