using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IInventoryRepository : IRepository<Repository>
{
	Task<Repository?> GetByUrlAsync(string url, CancellationToken cancellationToken);
}