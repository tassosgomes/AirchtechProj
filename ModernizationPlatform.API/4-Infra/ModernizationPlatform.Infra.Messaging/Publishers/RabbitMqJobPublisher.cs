using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Messaging.Messaging;

namespace ModernizationPlatform.Infra.Messaging.Publishers;

public sealed class RabbitMqJobPublisher : IJobPublisher
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<RabbitMqJobPublisher> _logger;

    public RabbitMqJobPublisher(IRabbitMqConnectionProvider connectionProvider, ILogger<RabbitMqJobPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
    {
        var connection = _connectionProvider.GetConnection();
        using var channel = connection.CreateModel();

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = message.JobId.ToString();
        properties.CorrelationId = message.RequestId.ToString();
        properties.Headers = new Dictionary<string, object>
        {
            [RabbitMqHeaders.RequestId] = message.RequestId.ToString(),
            [RabbitMqHeaders.CorrelationId] = message.RequestId.ToString()
        };

        var body = RabbitMqJsonSerializer.Serialize(message);

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqQueueNames.AnalysisJobs,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Job publicado em RabbitMQ. JobId: {JobId} RequestId: {RequestId}", message.JobId, message.RequestId);

        return Task.CompletedTask;
    }
}