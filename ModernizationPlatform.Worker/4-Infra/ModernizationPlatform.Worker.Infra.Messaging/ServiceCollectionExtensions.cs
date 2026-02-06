using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Infra.Messaging.Connection;
using ModernizationPlatform.Worker.Infra.Messaging.Consumers;
using ModernizationPlatform.Worker.Infra.Messaging.Health;
using ModernizationPlatform.Worker.Infra.Messaging.Publishers;
using ModernizationPlatform.Worker.Infra.Messaging.Setup;

namespace ModernizationPlatform.Worker.Infra.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddHostedService<RabbitMqQueueInitializer>();

        services.AddSingleton<IResultPublisher, RabbitMqResultPublisher>();

        services.AddSingleton<RabbitMqJobConsumer>();
        services.AddSingleton<IJobConsumer>(sp => sp.GetRequiredService<RabbitMqJobConsumer>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqJobConsumer>());

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}