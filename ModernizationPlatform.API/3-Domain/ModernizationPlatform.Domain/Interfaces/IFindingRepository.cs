using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IFindingRepository : IRepository<Finding>
{
    Task<IReadOnlyList<Finding>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken);
}