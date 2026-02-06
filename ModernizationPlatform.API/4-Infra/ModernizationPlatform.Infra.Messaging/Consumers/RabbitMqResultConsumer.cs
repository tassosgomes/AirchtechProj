using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Messaging.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ModernizationPlatform.Infra.Messaging.Consumers;

public sealed class RabbitMqResultConsumer : BackgroundService, IResultConsumer
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly IAnalysisResultHandler _handler;
    private readonly ILogger<RabbitMqResultConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IModel? _channel;

    public RabbitMqResultConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        IAnalysisResultHandler handler,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqResultConsumer> logger)
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
        var connection = _connectionProvider.GetConnection();
        _channel = connection.CreateModel();
        _channel.BasicQos(0, _options.PrefetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: RabbitMqQueueNames.AnalysisResults,
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
            var message = RabbitMqJsonSerializer.Deserialize<AnalysisResultMessage>(args.Body);
            if (message is null)
            {
                _logger.LogWarning("Mensagem inválida recebida em {Queue}", RabbitMqQueueNames.AnalysisResults);
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            await _handler.HandleAsync(message, CancellationToken.None);
            _channel.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar resultado do RabbitMQ.");
            _channel.BasicNack(args.DeliveryTag, false, requeue: true);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}