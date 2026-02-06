namespace ModernizationPlatform.Worker.Application.Messaging;

public static class RabbitMqRetryPolicy
{
    public static TimeSpan? GetDelay(int retryAttempt)
    {
        return retryAttempt switch
        {
            1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(30),
            _ => null
        };
    }
}