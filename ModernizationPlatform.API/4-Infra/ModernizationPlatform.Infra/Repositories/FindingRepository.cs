using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class FindingRepository : Repository<Finding>, IFindingRepository
{
    public FindingRepository(AppDbContext context)
        : base(context)
    {
    }
}