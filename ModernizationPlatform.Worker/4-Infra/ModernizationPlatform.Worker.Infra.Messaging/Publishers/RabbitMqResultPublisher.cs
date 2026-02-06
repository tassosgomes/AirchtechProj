using Microsoft.Extensions.Logging;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Infra.Messaging.Connection;
using ModernizationPlatform.Worker.Infra.Messaging.Messaging;

namespace ModernizationPlatform.Worker.Infra.Messaging.Publishers;

public sealed class RabbitMqResultPublisher : IResultPublisher
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<RabbitMqResultPublisher> _logger;

    public RabbitMqResultPublisher(IRabbitMqConnectionProvider connectionProvider, ILogger<RabbitMqResultPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public Task PublishResultAsync(AnalysisResultMessage message, CancellationToken cancellationToken)
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
            routingKey: RabbitMqQueueNames.AnalysisResults,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Resultado publicado em RabbitMQ. JobId: {JobId} RequestId: {RequestId}", message.JobId, message.RequestId);

        return Task.CompletedTask;
    }
}