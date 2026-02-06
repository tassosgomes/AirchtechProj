using RepositoryEntity = ModernizationPlatform.Domain.Entities.Repository;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class InventoryRepository : Repository<RepositoryEntity>, IInventoryRepository
{
    public InventoryRepository(AppDbContext context)
        : base(context)
    {
    }
}