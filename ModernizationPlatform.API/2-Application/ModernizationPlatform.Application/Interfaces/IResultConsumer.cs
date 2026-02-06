namespace ModernizationPlatform.Application.Interfaces;

public interface IResultConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}