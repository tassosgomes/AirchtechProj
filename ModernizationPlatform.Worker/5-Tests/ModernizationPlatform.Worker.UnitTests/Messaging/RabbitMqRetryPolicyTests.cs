using ModernizationPlatform.Worker.Application.Messaging;

namespace ModernizationPlatform.Worker.UnitTests.Messaging;

public class RabbitMqRetryPolicyTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 30)]
    public void GetDelay_Should_Return_Configured_Backoff(int attempt, int seconds)
    {
        var delay = RabbitMqRetryPolicy.GetDelay(attempt);

        Assert.NotNull(delay);
        Assert.Equal(TimeSpan.FromSeconds(seconds), delay);
    }

    [Fact]
    public void GetDelay_Should_Return_Null_When_Max_Attempts_Reached()
    {
        var delay = RabbitMqRetryPolicy.GetDelay(3);

        Assert.Null(delay);
    }
}