using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Persistence;
using RabbitMQ.Client;
using Xunit;

namespace ModernizationPlatform.API.IntegrationTests.Orchestration;

public class OrchestrationFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = "TestDb_Orchestration_" + Guid.NewGuid();

    public OrchestrationFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                var rabbitMqConnection = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqConnectionProvider));
                if (rabbitMqConnection != null) services.Remove(rabbitMqConnection);
                services.AddSingleton<IRabbitMqConnectionProvider, FakeRabbitMqConnectionProvider>();

                var jobPublisher = services.SingleOrDefault(d => d.ServiceType == typeof(IJobPublisher));
                if (jobPublisher != null) services.Remove(jobPublisher);
                services.AddSingleton<IJobPublisher, FakeJobPublisher>();

                var resultConsumer = services.SingleOrDefault(d => d.ServiceType == typeof(IResultConsumer));
                if (resultConsumer != null) services.Remove(resultConsumer);
                services.AddSingleton<IResultConsumer, FakeResultConsumer>();

                var discoveryService = services.SingleOrDefault(d => d.ServiceType == typeof(IDiscoveryService));
                if (discoveryService != null) services.Remove(discoveryService);
                services.AddSingleton<IDiscoveryService, FakeDiscoveryService>();

                var promptCatalog = services.SingleOrDefault(d => d.ServiceType == typeof(IPromptCatalogService));
                if (promptCatalog != null) services.Remove(promptCatalog);
                services.AddSingleton<IPromptCatalogService, FakePromptCatalogService>();

                var consolidationService = services.SingleOrDefault(d => d.ServiceType == typeof(IConsolidationService));
                if (consolidationService != null) services.Remove(consolidationService);
                services.AddSingleton<FakeConsolidationService>();
                services.AddSingleton<IConsolidationService>(sp => sp.GetRequiredService<FakeConsolidationService>());

                services.Configure<OrchestrationOptions>(options =>
                {
                    options.MaxParallelRequests = 1;
                    options.MaxJobRetries = 1;
                    options.JobTimeoutSeconds = 60;
                    options.PollingIntervalSeconds = 1;
                    options.FanOutDependencyThreshold = 0;
                    options.FanOutDependencyBatchSize = 50;
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task OrchestrationFlow_ShouldAdvanceToCompletion()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var orchestration = scope.ServiceProvider.GetRequiredService<IOrchestrationService>();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var consolidation = scope.ServiceProvider.GetRequiredService<FakeConsolidationService>();

        var request = await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                "token",
                [AnalysisType.Security, AnalysisType.Documentation]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        var updated = await requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Completed, updated!.Status);
        Assert.Contains(request.Id, consolidation.Requests);

        var jobs = await jobRepository.GetByRequestIdAsync(request.Id, CancellationToken.None);
        Assert.All(jobs, job => Assert.Equal(JobStatus.Completed, job.Status));
    }

    private sealed class FakeRabbitMqConnectionProvider : IRabbitMqConnectionProvider
    {
        public IConnection GetConnection() => null!;
    }

    private sealed class FakeJobPublisher : IJobPublisher
    {
        private readonly IAnalysisResultHandler _handler;
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();

        public FakeJobPublisher(IAnalysisResultHandler handler)
        {
            _handler = handler;
        }

        public async Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
        {
            var attempt = _attempts.AddOrUpdate(message.JobId, 1, (_, current) => current + 1);
            var status = attempt == 1 ? "COMPLETED" : "COMPLETED";

            var running = new AnalysisResultMessage(
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                "RUNNING",
                "{}",
                0,
                null);

            var completed = new AnalysisResultMessage(
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                status,
                JsonSerializer.Serialize(new { ok = true }),
                10,
                null);

            await _handler.HandleAsync(running, cancellationToken);
            await _handler.HandleAsync(completed, cancellationToken);
        }
    }

    private sealed class FakeResultConsumer : IResultConsumer
    {
        public Task StartConsumingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDiscoveryService : IDiscoveryService
    {
        public Task<SharedContext> ExecuteDiscoveryAsync(
            AnalysisRequest request,
            string? accessToken,
            CancellationToken cancellationToken)
        {
            var sharedContext = new SharedContext(
                request.Id,
                1,
                ["C#"],
                ["ASP.NET Core"],
                ["Package@1.0"],
                "{}");

            return Task.FromResult(sharedContext);
        }
    }

    private sealed class FakePromptCatalogService : IPromptCatalogService
    {
        public Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Prompt>>(new List<Prompt>());

        public Task<Prompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Prompt?>(null);

        public Task<Prompt?> GetByAnalysisTypeAsync(AnalysisType analysisType, CancellationToken cancellationToken = default)
            => Task.FromResult<Prompt?>(new Prompt(analysisType, $"Prompt for {analysisType}"));

        public Task<Prompt> CreateOrUpdateAsync(AnalysisType analysisType, string content, CancellationToken cancellationToken = default)
            => Task.FromResult(new Prompt(analysisType, content));

        public Task<Prompt?> UpdateAsync(Guid id, string content, CancellationToken cancellationToken = default)
            => Task.FromResult<Prompt?>(new Prompt(AnalysisType.Security, content));
    }

    private sealed class FakeConsolidationService : IConsolidationService
    {
        public List<Guid> Requests { get; } = new();

        public Task ConsolidateAsync(Guid requestId, CancellationToken cancellationToken)
        {
            Requests.Add(requestId);
            return Task.CompletedTask;
        }
    }
}
