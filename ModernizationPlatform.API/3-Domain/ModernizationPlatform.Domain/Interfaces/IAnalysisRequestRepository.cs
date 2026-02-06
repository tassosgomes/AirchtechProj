using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IAnalysisRequestRepository : IRepository<AnalysisRequest>
{
	Task<IReadOnlyList<AnalysisRequest>> GetPagedAsync(int page, int size, CancellationToken cancellationToken);
	Task<int> CountAsync(CancellationToken cancellationToken);
	Task<int> CountQueuedBeforeAsync(DateTime createdAt, CancellationToken cancellationToken);
}