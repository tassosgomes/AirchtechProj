using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Messaging.Messaging;

namespace ModernizationPlatform.Infra.Messaging.Setup;

public sealed class RabbitMqQueueInitializer : IHostedService
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<RabbitMqQueueInitializer> _logger;

    public RabbitMqQueueInitializer(IRabbitMqConnectionProvider connectionProvider, ILogger<RabbitMqQueueInitializer> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var connection = _connectionProvider.GetConnection();
        using var channel = connection.CreateModel();

        var jobsArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = RabbitMqQueueNames.AnalysisJobsDlq
        };

        var dlqArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = RabbitMqQueueNames.AnalysisJobs
        };

        channel.QueueDeclare(RabbitMqQueueNames.AnalysisJobs, durable: true, exclusive: false, autoDelete: false, arguments: jobsArgs);
        channel.QueueDeclare(RabbitMqQueueNames.AnalysisResults, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(RabbitMqQueueNames.AnalysisJobsDlq, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);

        _logger.LogInformation("RabbitMQ filas declaradas: {JobsQueue}, {ResultsQueue}, {DlqQueue}",
            RabbitMqQueueNames.AnalysisJobs,
            RabbitMqQueueNames.AnalysisResults,
            RabbitMqQueueNames.AnalysisJobsDlq);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}