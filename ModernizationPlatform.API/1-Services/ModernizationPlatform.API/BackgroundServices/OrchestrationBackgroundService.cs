using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.Interfaces;

namespace ModernizationPlatform.API.BackgroundServices;

public sealed class OrchestrationBackgroundService : BackgroundService
{
    private readonly IOrchestrationService _orchestrationService;
    private readonly OrchestrationOptions _options;
    private readonly ILogger<OrchestrationBackgroundService> _logger;

    public OrchestrationBackgroundService(
        IOrchestrationService orchestrationService,
        IOptions<OrchestrationOptions> options,
        ILogger<OrchestrationBackgroundService> logger)
    {
        _orchestrationService = orchestrationService ?? throw new ArgumentNullException(nameof(orchestrationService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Orchestration background service iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _orchestrationService.ProcessPendingRequestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar fila de requests.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Orchestration background service finalizado.");
    }
}
