using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Domain.Services;
using ModernizationPlatform.Infra.Discovery;
using ModernizationPlatform.Infra.Persistence;
using Xunit;

namespace ModernizationPlatform.API.IntegrationTests.Discovery;

/// <summary>
/// Integration test for the Discovery service using a real public repository
/// Note: Requires git to be installed and internet connectivity
/// </summary>
public class DiscoveryServiceIntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private IDiscoveryService? _discoveryService;

    public Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discovery:CloneTimeoutMinutes"] = "5"
            })
            .Build();

        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add Discovery services
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<IGitCloneService, GitCloneService>();
        services.AddScoped<ILanguageDetectorService, LanguageDetectorService>();
        services.AddScoped<IDotNetProjectAnalyzer, DotNetProjectAnalyzer>();
        services.AddScoped<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddScoped<IDirectoryStructureMapper, DirectoryStructureMapper>();

        // Mock repositories
        services.AddScoped<IRepository<SharedContext>, MockSharedContextRepository>();
        services.AddScoped<IUnitOfWork, MockUnitOfWork>();

        _serviceProvider = services.BuildServiceProvider();
        _discoveryService = _serviceProvider.GetRequiredService<IDiscoveryService>();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact(Skip = "Integration test - requires internet and git")]
    public async Task ExecuteDiscoveryAsync_WithPublicRepository_GeneratesSharedContext()
    {
        // Arrange: Use a small, stable public repository for testing
        // Using a simple .NET template repository
        var request = new AnalysisRequest(
            repositoryUrl: "https://github.com/dotnet/blazor-samples",
            provider: SourceProvider.GitHub,
            selectedTypes: new[] { AnalysisType.Obsolescence });

        // Act
        var result = await _discoveryService!.ExecuteDiscoveryAsync(
            request,
            accessToken: null, // Public repository
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Id, result.RequestId);
        Assert.True(result.Languages.Count > 0, "Should detect at least one language");
        Assert.NotEmpty(result.DirectoryStructureJson);
        
        // Should detect C# in a .NET repository
        Assert.Contains(result.Languages, l => l == "C#" || l == ".NET");
    }

    // Mock implementations for testing
    private class MockSharedContextRepository : IRepository<SharedContext>
    {
        private readonly List<SharedContext> _contexts = new();

        public Task<SharedContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_contexts.FirstOrDefault(c => c.Id == id));
        }

        public Task<IReadOnlyList<SharedContext>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SharedContext>>(_contexts.AsReadOnly());
        }

        public Task AddAsync(SharedContext entity, CancellationToken cancellationToken)
        {
            _contexts.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(SharedContext entity)
        {
            // No-op for mock
        }

        public void Delete(SharedContext entity)
        {
            _contexts.Remove(entity);
        }
    }

    private class MockUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
