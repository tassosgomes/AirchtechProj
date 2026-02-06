using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Infra.CopilotSdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotSdk(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CopilotSdkOptions>()
            .BindConfiguration(CopilotSdkOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CopilotSdkOptions>>().Value;
            var client = new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }

            return client;
        });

        services.AddSingleton<ICopilotClient, CopilotClient>();
        services.AddSingleton<IGitCloneService, GitCloneService>();

        return services;
    }
}
