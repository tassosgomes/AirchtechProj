using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModernizationPlatform.Worker.Infra.Messaging.Connection;

namespace ModernizationPlatform.Worker.Infra.Messaging.Health;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;

    public RabbitMqHealthCheck(IRabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = _connectionProvider.GetConnection();
            return Task.FromResult(connection.IsOpen
                ? HealthCheckResult.Healthy("Conexão RabbitMQ aberta")
                : HealthCheckResult.Unhealthy("Conexão RabbitMQ fechada"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Falha ao conectar no RabbitMQ", ex));
        }
    }
}