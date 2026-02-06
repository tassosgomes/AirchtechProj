using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class NoOpConsolidationService : IConsolidationService
{
    private readonly ILogger<NoOpConsolidationService> _logger;

    public NoOpConsolidationService(ILogger<NoOpConsolidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ConsolidateAsync(Guid requestId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Consolidacao ainda nao implementada. RequestId: {RequestId}", requestId);
        return Task.CompletedTask;
    }
}
