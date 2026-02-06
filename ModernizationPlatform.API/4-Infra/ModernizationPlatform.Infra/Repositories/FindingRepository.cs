using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<Finding>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken)
    {
        // Buscar todos os findings associados aos jobs deste request
        var findings = await Context.Findings
            .Where(f => Context.AnalysisJobs.Any(j => j.Id == f.JobId && j.RequestId == requestId))
            .OrderByDescending(f => f.Severity)
            .ToListAsync(cancellationToken);

        return findings;
    }
}