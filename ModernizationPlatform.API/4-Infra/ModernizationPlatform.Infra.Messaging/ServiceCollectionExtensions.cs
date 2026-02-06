using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Messaging.Consumers;
using ModernizationPlatform.Infra.Messaging.Publishers;
using ModernizationPlatform.Infra.Messaging.Setup;

namespace ModernizationPlatform.Infra.Messaging;

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

        services.AddSingleton<IJobPublisher, RabbitMqJobPublisher>();

        services.AddSingleton<RabbitMqResultConsumer>();
        services.AddSingleton<IResultConsumer>(sp => sp.GetRequiredService<RabbitMqResultConsumer>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqResultConsumer>());

        return services;
    }
}