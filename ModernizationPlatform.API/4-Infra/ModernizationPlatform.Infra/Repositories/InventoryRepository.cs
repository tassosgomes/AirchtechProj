using RepositoryEntity = ModernizationPlatform.Domain.Entities.Repository;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class InventoryRepository : Repository<RepositoryEntity>, IInventoryRepository
{
    public InventoryRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<RepositoryEntity?> GetByUrlAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(repository => repository.Url == url, cancellationToken);
    }
}