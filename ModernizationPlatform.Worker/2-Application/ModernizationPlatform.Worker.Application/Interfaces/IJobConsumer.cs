namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IJobConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}