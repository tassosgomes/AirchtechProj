using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Application.Interfaces;

public interface IOrchestrationService
{
    Task<AnalysisRequest> CreateRequestAsync(CreateAnalysisCommand command, CancellationToken cancellationToken);
    Task ProcessPendingRequestsAsync(CancellationToken cancellationToken);
}
