namespace ModernizationPlatform.Application.Interfaces;

public interface IConsolidationService
{
    Task ConsolidateAsync(Guid requestId, CancellationToken cancellationToken);
}
