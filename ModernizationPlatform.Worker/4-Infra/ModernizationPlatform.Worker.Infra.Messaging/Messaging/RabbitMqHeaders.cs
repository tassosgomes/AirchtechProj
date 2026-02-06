namespace ModernizationPlatform.Worker.Infra.Messaging.Messaging;

public static class RabbitMqHeaders
{
    public const string RequestId = "requestId";
    public const string CorrelationId = "correlationId";
    public const string RetryCount = "retry-count";

    public static int GetRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCount, out var value) || value is null)
        {
            return 0;
        }

        if (value is byte[] bytes && int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var parsed))
        {
            return parsed;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is long longValue)
        {
            return (int)longValue;
        }

        return 0;
    }
}