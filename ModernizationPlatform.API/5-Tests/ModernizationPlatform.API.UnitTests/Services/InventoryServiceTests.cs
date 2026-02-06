using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;
using ModernizationPlatform.Infra.Repositories;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Services;

public sealed class InventoryServiceTests
{
    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<AppDbContext>(db =>
        {
            db.UseInMemoryDatabase(dbName);
            db.EnableSensitiveDataLogging();
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<InventoryService>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    [Fact]
    public async Task QueryAsync_WithTechnologyFilter_ReturnsMatchingRepositories()
    {
        var provider = BuildProvider("Test_" + Guid.NewGuid());
        await using var scope = provider.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<InventoryService>();
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-a", "repo-a",
            new[] { "C#" }, new[] { "ASP.NET Core 8 (Framework)" }, new[] { "Newtonsoft.Json@13.0 (NuGet)" },
            Severity.High);
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-b", "repo-b",
            new[] { "Java" }, new[] { "Spring Boot 3 (Framework)" }, new[] { "JUnit@5 (Maven)" },
            Severity.Medium);

        var filter = new InventoryFilter("ASP.NET Core", null, null, null, null, 1, 10);

        var result = await sut.QueryAsync(filter, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("repo-a");
    }

    [Fact]
    public async Task QueryAsync_WithDependencyFilter_ReturnsMatchingRepositories()
    {
        var provider = BuildProvider("Test_" + Guid.NewGuid());
        await using var scope = provider.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<InventoryService>();
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-a", "repo-a",
            new[] { "C#" }, new[] { "ASP.NET Core 8 (Framework)" }, new[] { "Serilog@3.0 (NuGet)" },
            Severity.Low);
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-b", "repo-b",
            new[] { "Java" }, new[] { "Spring Boot 3 (Framework)" }, new[] { "JUnit@5 (Maven)" },
            Severity.Medium);

        var filter = new InventoryFilter(null, "JUnit", null, null, null, 1, 10);

        var result = await sut.QueryAsync(filter, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("repo-b");
    }

    [Fact]
    public async Task QueryAsync_WithSeverityFilter_ReturnsRepositoriesWithMatchingFindings()
    {
        var provider = BuildProvider("Test_" + Guid.NewGuid());
        await using var scope = provider.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<InventoryService>();
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-a", "repo-a",
            new[] { "C#" }, new[] { "ASP.NET Core 8 (Framework)" }, new[] { "Serilog@3.0 (NuGet)" },
            Severity.Medium);
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-b", "repo-b",
            new[] { "Java" }, new[] { "Spring Boot 3 (Framework)" }, new[] { "JUnit@5 (Maven)" },
            Severity.Critical);

        var filter = new InventoryFilter(null, null, Severity.High, null, null, 1, 10);

        var result = await sut.QueryAsync(filter, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("repo-b");
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ReturnsPagedResults()
    {
        var provider = BuildProvider("Test_" + Guid.NewGuid());
        await using var scope = provider.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<InventoryService>();
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-a", "repo-a",
            new[] { "C#" }, new[] { "ASP.NET Core 8 (Framework)" }, new[] { "Serilog@3.0 (NuGet)" },
            Severity.Medium);
        await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-b", "repo-b",
            new[] { "Java" }, new[] { "Spring Boot 3 (Framework)" }, new[] { "JUnit@5 (Maven)" },
            Severity.Medium);

        var filter = new InventoryFilter(null, null, null, null, null, 1, 1);

        var result = await sut.QueryAsync(filter, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Pagination.Total.Should().Be(2);
        result.Pagination.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetTimelineAsync_WithRepository_ReturnsOrderedTimeline()
    {
        var provider = BuildProvider("Test_" + Guid.NewGuid());
        await using var scope = provider.CreateAsyncScope();

        var sut = scope.ServiceProvider.GetRequiredService<InventoryService>();
        var repository = await SeedRepositoryAsync(scope.ServiceProvider, "https://github.com/org/repo-a", "repo-a",
            new[] { "C#" }, new[] { "ASP.NET Core 8 (Framework)" }, new[] { "Serilog@3.0 (NuGet)" },
            Severity.High);

        await Task.Delay(5);

        await SeedAdditionalAnalysisAsync(scope.ServiceProvider, repository.Url, Severity.Low);

        var timeline = await sut.GetTimelineAsync(repository.Id, CancellationToken.None);

        timeline.Should().NotBeNull();
        timeline!.Analyses.Should().HaveCount(2);
        timeline.Analyses[0].Summary[Severity.High.ToString()].Should().Be(1);
    }

    private static async Task<Repository> SeedRepositoryAsync(
        IServiceProvider provider,
        string url,
        string name,
        IReadOnlyList<string> languages,
        IReadOnlyList<string> frameworks,
        IReadOnlyList<string> dependencies,
        Severity severity)
    {
        var inventoryRepo = provider.GetRequiredService<IInventoryRepository>();
        var requestRepo = provider.GetRequiredService<IAnalysisRequestRepository>();
        var sharedContextRepo = provider.GetRequiredService<IRepository<SharedContext>>();
        var jobRepo = provider.GetRequiredService<IRepository<AnalysisJob>>();
        var findingRepo = provider.GetRequiredService<IRepository<Finding>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        var repository = new Repository(url, name, SourceProvider.GitHub);
        repository.MarkAnalyzed(DateTime.UtcNow);
        await inventoryRepo.AddAsync(repository, CancellationToken.None);

        var request = new AnalysisRequest(url, SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        request.Complete();
        await requestRepo.AddAsync(request, CancellationToken.None);

        var sharedContext = new SharedContext(
            request.Id,
            1,
            languages,
            frameworks,
            dependencies,
            "{}");
        await sharedContextRepo.AddAsync(sharedContext, CancellationToken.None);

        var job = new AnalysisJob(request.Id, AnalysisType.Security);
        job.Start();
        job.Complete("{}", TimeSpan.FromSeconds(1));
        await jobRepo.AddAsync(job, CancellationToken.None);

        var finding = new Finding(job.Id, severity, "Security", "Finding", "Description", "file.cs");
        await findingRepo.AddAsync(finding, CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return repository;
    }

    private static async Task SeedAdditionalAnalysisAsync(IServiceProvider provider, string url, Severity severity)
    {
        var requestRepo = provider.GetRequiredService<IAnalysisRequestRepository>();
        var sharedContextRepo = provider.GetRequiredService<IRepository<SharedContext>>();
        var jobRepo = provider.GetRequiredService<IRepository<AnalysisJob>>();
        var findingRepo = provider.GetRequiredService<IRepository<Finding>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        var request = new AnalysisRequest(url, SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        request.Complete();
        await requestRepo.AddAsync(request, CancellationToken.None);

        var sharedContext = new SharedContext(
            request.Id,
            1,
            new[] { "C#" },
            new[] { "ASP.NET Core 8 (Framework)" },
            new[] { "Serilog@3.0 (NuGet)" },
            "{}");
        await sharedContextRepo.AddAsync(sharedContext, CancellationToken.None);

        var job = new AnalysisJob(request.Id, AnalysisType.Security);
        job.Start();
        job.Complete("{}", TimeSpan.FromSeconds(1));
        await jobRepo.AddAsync(job, CancellationToken.None);

        var finding = new Finding(job.Id, severity, "Security", "Another", "Description", "file.cs");
        await findingRepo.AddAsync(finding, CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
