using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Application.Configuration;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ModernizationPlatform.Infra.Messaging.Connection;

public interface IRabbitMqConnectionProvider
{
    IConnection GetConnection();
}

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IDisposable
{
    private readonly Lazy<IConnection> _connection;
    private readonly ILogger<RabbitMqConnectionProvider> _logger;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnectionProvider> logger)
    {
        _logger = logger;
        var settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.Username,
            Password = settings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        var policy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (exception, delay, _, _) =>
                {
                    _logger.LogWarning(exception, "Falha ao conectar no RabbitMQ. Tentando novamente em {DelaySeconds}s", delay.TotalSeconds);
                });

        _connection = new Lazy<IConnection>(() => policy.Execute(factory.CreateConnection));
    }

    public IConnection GetConnection() => _connection.Value;

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}