using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Interfaces;

public interface IInventoryService
{
    Task<PagedResult<RepositorySummary>> QueryAsync(InventoryFilter filter, CancellationToken cancellationToken);
    Task<RepositoryTimeline?> GetTimelineAsync(Guid repositoryId, CancellationToken cancellationToken);
    Task<PagedResult<FindingSummary>> QueryFindingsAsync(InventoryFilter filter, CancellationToken cancellationToken);
}
