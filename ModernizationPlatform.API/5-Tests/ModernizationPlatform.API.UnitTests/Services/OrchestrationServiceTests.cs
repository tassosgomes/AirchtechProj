using System.Collections.Concurrent;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Application.Validators;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;
using ModernizationPlatform.Infra.Repositories;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Services;

public class OrchestrationServiceTests
{
    private static ServiceProvider BuildProvider(OrchestrationTestContext context, OrchestrationOptions options, string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Registrar DbContext com o nome do banco capturado para garantir compartilhamento entre scopes
        services.AddDbContext<AppDbContext>(db =>
        {
            db.UseInMemoryDatabase(dbName);
            db.EnableSensitiveDataLogging();
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
        services.AddScoped<IPromptRepository, PromptRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<FluentValidation.IValidator<CreateAnalysisCommand>, CreateAnalysisCommandValidator>();
        services.AddSingleton(context);
        services.AddSingleton<OrchestrationStateStore>();
        services.AddSingleton<IAnalysisResultHandler, OrchestrationResultHandler>();
        services.AddSingleton<IJobPublisher, TestJobPublisher>();
        services.AddSingleton<IPromptCatalogService, FakePromptCatalogService>();
        services.AddSingleton<IDiscoveryService, FakeDiscoveryService>();
        services.AddSingleton<IConsolidationService, FakeConsolidationService>();
        services.AddSingleton<IOrchestrationService, OrchestrationService>();
        services.AddSingleton<IOptions<OrchestrationOptions>>(Options.Create(options));

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldPersistRequestAndStoreAccessToken()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext();
        var provider = BuildProvider(context, new OrchestrationOptions(), dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();
        var stateStore = provider.GetRequiredService<OrchestrationStateStore>();

        var command = new CreateAnalysisCommand(
            "https://github.com/org/repo",
            SourceProvider.GitHub,
            "token-123",
            [AnalysisType.Security]);

        var request = await orchestration.CreateRequestAsync(command, CancellationToken.None);

        var storedToken = stateStore.GetAccessToken(request.Id);
        Assert.Equal("token-123", storedToken);

        await using var scope = provider.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var loaded = await repo.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(RequestStatus.Queued, loaded!.Status);
    }

    [Fact]
    public async Task ProcessPendingRequestsAsync_ShouldCompleteRequestAndJobs()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext();
        var provider = BuildProvider(context, new OrchestrationOptions(), dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();

        var request = await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                "token-123",
                [AnalysisType.Security, AnalysisType.Documentation]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();

        var updated = await requestRepo.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Completed, updated!.Status);

        var jobs = await jobRepo.GetByRequestIdAsync(request.Id, CancellationToken.None);
        Assert.All(jobs, job => Assert.Equal(JobStatus.Completed, job.Status));
    }

    [Fact]
    public async Task ProcessPendingRequestsAsync_ShouldFailRequest_WhenDiscoveryFails()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext { FailDiscovery = true };
        var provider = BuildProvider(context, new OrchestrationOptions(), dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();

        var request = await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                null,
                [AnalysisType.Security]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var updated = await requestRepo.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Failed, updated!.Status);
    }

    [Fact]
    public async Task ProcessPendingRequestsAsync_ShouldRetryJobUntilSuccess()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext();
        context.SetFailurePlan(failuresBeforeSuccess: 1);

        var provider = BuildProvider(context, new OrchestrationOptions { MaxJobRetries = 1 }, dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();

        var request = await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                "token-123",
                [AnalysisType.Security]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();

        var updated = await requestRepo.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Completed, updated!.Status);

        var jobs = await jobRepo.GetByRequestIdAsync(request.Id, CancellationToken.None);
        Assert.All(jobs, job => Assert.Equal(JobStatus.Completed, job.Status));
        Assert.Equal(2, context.PublishedMessages.Count);
    }

    [Fact]
    public async Task ProcessPendingRequestsAsync_ShouldStopAfterMaxRetries()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext();
        context.SetFailurePlan(failuresBeforeSuccess: 5);

        var provider = BuildProvider(context, new OrchestrationOptions { MaxJobRetries = 1 }, dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();

        var request = await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                null,
                [AnalysisType.Security]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        await using var scope = provider.CreateAsyncScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();

        var updated = await requestRepo.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(RequestStatus.Failed, updated!.Status);

        var jobs = await jobRepo.GetByRequestIdAsync(request.Id, CancellationToken.None);
        Assert.All(jobs, job => Assert.Equal(JobStatus.Failed, job.Status));
        Assert.Equal(2, context.PublishedMessages.Count);
    }

    [Fact]
    public async Task ProcessPendingRequestsAsync_ShouldPublishJobsInSelectedTypeOrder()
    {
        // Arrange: banco isolado para este teste
        var dbName = "Test_" + Guid.NewGuid().ToString();
        var context = new OrchestrationTestContext();
        var provider = BuildProvider(context, new OrchestrationOptions(), dbName);
        var orchestration = provider.GetRequiredService<IOrchestrationService>();

        await orchestration.CreateRequestAsync(
            new CreateAnalysisCommand(
                "https://github.com/org/repo",
                SourceProvider.GitHub,
                null,
                [AnalysisType.Observability, AnalysisType.Security, AnalysisType.Documentation]),
            CancellationToken.None);

        await orchestration.ProcessPendingRequestsAsync(CancellationToken.None);

        var publishedTypes = context.PublishedMessages.Select(m => m.AnalysisType).ToList();
        Assert.Equal(new[] { "Observability", "Security", "Documentation" }, publishedTypes);
    }

    private sealed class FakeDiscoveryService : IDiscoveryService
    {
        private readonly OrchestrationTestContext _context;

        public FakeDiscoveryService(OrchestrationTestContext context)
        {
            _context = context;
        }

        public Task<SharedContext> ExecuteDiscoveryAsync(
            AnalysisRequest request,
            string? accessToken,
            CancellationToken cancellationToken)
        {
            if (_context.FailDiscovery)
            {
                throw new InvalidOperationException("Discovery failed");
            }

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
        public Task ConsolidateAsync(Guid requestId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestJobPublisher : IJobPublisher
    {
        private readonly OrchestrationTestContext _context;
        private readonly IAnalysisResultHandler _handler;

        public TestJobPublisher(OrchestrationTestContext context, IAnalysisResultHandler handler)
        {
            _context = context;
            _handler = handler;
        }

        public async Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
        {
            _context.PublishedMessages.Add(message);

            foreach (var result in _context.ResultPlan(message))
            {
                await _handler.HandleAsync(result, cancellationToken);
            }
        }
    }

    private sealed class OrchestrationTestContext
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();

        public List<AnalysisJobMessage> PublishedMessages { get; } = new();
        public bool FailDiscovery { get; set; }
        public Func<AnalysisJobMessage, IEnumerable<AnalysisResultMessage>> ResultPlan { get; private set; }

        public OrchestrationTestContext()
        {
            ResultPlan = CreatePlan(0);
        }

        public void SetFailurePlan(int failuresBeforeSuccess)
        {
            ResultPlan = CreatePlan(failuresBeforeSuccess);
        }

        private Func<AnalysisJobMessage, IEnumerable<AnalysisResultMessage>> CreatePlan(int failuresBeforeSuccess)
        {
            return message =>
            {
                var attempt = _attempts.AddOrUpdate(message.JobId, 1, (_, current) => current + 1);
                var isFailure = attempt <= failuresBeforeSuccess;

                var running = new AnalysisResultMessage(
                    message.JobId,
                    message.RequestId,
                    message.AnalysisType,
                    "RUNNING",
                    "{}",
                    0,
                    null);

                var final = new AnalysisResultMessage(
                    message.JobId,
                    message.RequestId,
                    message.AnalysisType,
                    isFailure ? "FAILED" : "COMPLETED",
                    "{}",
                    10,
                    isFailure ? "error" : null);

                return new[] { running, final };
            };
        }
    }
}
