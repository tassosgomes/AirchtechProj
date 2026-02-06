using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ModernizationPlatform.Worker;

public sealed class HealthCheckBackgroundService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckBackgroundService> _logger;

    public HealthCheckBackgroundService(HealthCheckService healthCheckService, ILogger<HealthCheckBackgroundService> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var report = await _healthCheckService.CheckHealthAsync(stoppingToken);
            _logger.LogInformation("Health checks executados. Status: {Status}", report.Status);

            foreach (var entry in report.Entries)
            {
                _logger.LogInformation("HealthCheck {Name}: {Status}", entry.Key, entry.Value.Status);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}