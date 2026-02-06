using Microsoft.EntityFrameworkCore;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class PromptRepository : Repository<Prompt>, IPromptRepository
{
    public PromptRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<Prompt?> GetByAnalysisTypeAsync(AnalysisType analysisType, CancellationToken cancellationToken)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AnalysisType == analysisType, cancellationToken);
    }
}