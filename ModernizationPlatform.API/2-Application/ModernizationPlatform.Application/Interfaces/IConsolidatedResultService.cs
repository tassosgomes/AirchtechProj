using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Interfaces;

public interface IConsolidatedResultService
{
    Task<ConsolidatedResultDto?> GetConsolidatedResultAsync(Guid requestId, CancellationToken cancellationToken);
}
