using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IAnalysisJobRepository : IRepository<AnalysisJob>
{
    Task<IReadOnlyList<AnalysisJob>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken);
}
