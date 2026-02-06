using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class AnalysisJobRepository : Repository<AnalysisJob>, IAnalysisJobRepository
{
    public AnalysisJobRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AnalysisJob>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await DbSet.AsNoTracking()
            .Where(job => job.RequestId == requestId)
            .OrderBy(job => job.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
