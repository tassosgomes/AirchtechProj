using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class AnalysisRequestRepository : Repository<AnalysisRequest>, IAnalysisRequestRepository
{
    public AnalysisRequestRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<AnalysisRequest>> GetPagedAsync(int page, int size, CancellationToken cancellationToken)
    {
        return await DbSet.AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisRequest>> GetQueuedAsync(int size, CancellationToken cancellationToken)
    {
        return await DbSet.AsNoTracking()
            .Where(request => request.Status == RequestStatus.Queued)
            .OrderBy(request => request.CreatedAt)
            .Take(size)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return await DbSet.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<int> CountQueuedBeforeAsync(DateTime createdAt, CancellationToken cancellationToken)
    {
        return await DbSet.AsNoTracking()
            .CountAsync(request => request.Status == RequestStatus.Queued && request.CreatedAt < createdAt, cancellationToken);
    }
}