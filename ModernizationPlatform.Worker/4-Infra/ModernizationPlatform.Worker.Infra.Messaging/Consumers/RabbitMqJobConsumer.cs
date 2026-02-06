using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Application.Messaging;
using ModernizationPlatform.Worker.Infra.Messaging.Connection;
using ModernizationPlatform.Worker.Infra.Messaging.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ModernizationPlatform.Worker.Infra.Messaging.Consumers;

public sealed class RabbitMqJobConsumer : BackgroundService, IJobConsumer
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly IAnalysisJobHandler _handler;
    private readonly ILogger<RabbitMqJobConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    public RabbitMqJobConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        IAnalysisJobHandler handler,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqJobConsumer> logger)
    {
        _connectionProvider = connectionProvider;
        _handler = handler;
        _logger = logger;
        _options = options.Value;
    }

    public Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        return ExecuteAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        var connection = _connectionProvider.GetConnection();
        _channel = connection.CreateModel();
        _channel.BasicQos(0, _options.PrefetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: RabbitMqQueueNames.AnalysisJobs,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var message = RabbitMqJsonSerializer.Deserialize<AnalysisJobMessage>(args.Body);
            if (message is null)
            {
                _logger.LogWarning("Mensagem inválida recebida em {Queue}", RabbitMqQueueNames.AnalysisJobs);
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            await _handler.HandleAsync(message, _stoppingToken);
            _channel.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar job do RabbitMQ.");
            await HandleProcessingFailureAsync(args);
        }
    }

    private Task HandleProcessingFailureAsync(BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return Task.CompletedTask;
        }

        var currentRetry = RabbitMqHeaders.GetRetryCount(args.BasicProperties?.Headers);
        var nextRetry = currentRetry + 1;
        var delay = RabbitMqRetryPolicy.GetDelay(nextRetry);

        if (delay is null)
        {
            _channel.BasicNack(args.DeliveryTag, false, requeue: false);
            return Task.CompletedTask;
        }

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = args.BasicProperties?.ContentType ?? "application/json";
        properties.MessageId = args.BasicProperties?.MessageId;
        properties.CorrelationId = args.BasicProperties?.CorrelationId;
        properties.Headers = new Dictionary<string, object>(args.BasicProperties?.Headers ?? new Dictionary<string, object>())
        {
            [RabbitMqHeaders.RetryCount] = nextRetry
        };
        properties.Expiration = ((int)delay.Value.TotalMilliseconds).ToString();

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqQueueNames.AnalysisJobsDlq,
            mandatory: false,
            basicProperties: properties,
            body: args.Body);

        _channel.BasicAck(args.DeliveryTag, false);

        _logger.LogWarning("Job reencaminhado para DLQ com backoff {DelaySeconds}s. Tentativa {Retry}", delay.Value.TotalSeconds, nextRetry);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}