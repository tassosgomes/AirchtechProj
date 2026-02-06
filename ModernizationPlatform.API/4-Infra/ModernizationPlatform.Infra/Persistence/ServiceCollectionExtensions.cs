using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Repositories;

namespace ModernizationPlatform.Infra.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
        services.AddScoped<IPromptRepository, PromptRepository>();
        services.AddScoped<IFindingRepository, FindingRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        return services;
    }
}