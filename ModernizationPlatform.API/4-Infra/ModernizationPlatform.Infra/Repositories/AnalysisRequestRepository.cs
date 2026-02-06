using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.Infra.Repositories;

public sealed class AnalysisRequestRepository : Repository<AnalysisRequest>, IAnalysisRequestRepository
{
    public AnalysisRequestRepository(AppDbContext context)
        : base(context)
    {
    }
}