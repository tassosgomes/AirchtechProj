using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;
using ModernizationPlatform.Infra.Repositories;
using Xunit;

namespace ModernizationPlatform.API.UnitTests.Services;

public sealed class ConsolidationServiceTests
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
        services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
        services.AddScoped<IFindingRepository, FindingRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ConsolidationService>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    [Fact]
    public async Task ConsolidateAsync_RequestNotFound_ThrowsException()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidationService>();

        var requestId = Guid.NewGuid();

        // Act
        var act = async () => await sut.ConsolidateAsync(requestId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Request {requestId} not found.");
    }

    [Fact]
    public async Task ConsolidateAsync_WithValidFindings_CreatesFindings()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();
        
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var findingRepo = scope.ServiceProvider.GetRequiredService<IFindingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidationService>();

        var request = new AnalysisRequest("https://github.com/test/repo", SourceProvider.GitHub, [AnalysisType.Obsolescence]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepo.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var job = new AnalysisJob(request.Id, AnalysisType.Obsolescence);
        job.Start();

        var outputJson = JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new
                {
                    severity = "High",
                    category = "Obsolescence",
                    title = "Outdated dependency",
                    description = "Package X is outdated",
                    filePath = "src/project.csproj"
                },
                new
                {
                    severity = "Medium",
                    category = "Security",
                    title = "Potential vulnerability",
                    description = "Package Y has known CVE",
                    filePath = "src/project.csproj"
                }
            }
        });

        job.Complete(outputJson, TimeSpan.FromSeconds(10));
        await jobRepo.AddAsync(job, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await sut.ConsolidateAsync(request.Id, CancellationToken.None);

        // Assert
        var findings = await findingRepo.GetByRequestIdAsync(request.Id, CancellationToken.None);
        findings.Should().HaveCount(2);
        findings.Should().Contain(f => f.Severity == Severity.High && f.Category == "Obsolescence");
        findings.Should().Contain(f => f.Severity == Severity.Medium && f.Category == "Security");
    }

    [Fact]
    public async Task ConsolidateAsync_WithCompletedRequest_UpsertsRepositoryInventory()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();

        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var inventoryRepo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidationService>();

        var request = new AnalysisRequest("https://github.com/test/inventory-repo", SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepo.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var job = new AnalysisJob(request.Id, AnalysisType.Security);
        job.Start();
        job.Complete(JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new { severity = "High", category = "Security", title = "CVE", description = "Issue", filePath = "file.cs" }
            }
        }), TimeSpan.FromSeconds(5));
        await jobRepo.AddAsync(job, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await sut.ConsolidateAsync(request.Id, CancellationToken.None);

        // Assert
        var repository = await inventoryRepo.GetByUrlAsync(request.RepositoryUrl, CancellationToken.None);
        repository.Should().NotBeNull();
        repository!.LastAnalysisAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConsolidateAsync_MultipleJobs_ConsolidatesAllFindings()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();
        
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var findingRepo = scope.ServiceProvider.GetRequiredService<IFindingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidationService>();

        var request = new AnalysisRequest("https://github.com/test/repo", SourceProvider.GitHub, 
            [AnalysisType.Obsolescence, AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepo.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        
        var job1 = new AnalysisJob(request.Id, AnalysisType.Obsolescence);
        job1.Start();
        job1.Complete(JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new { severity = "High", category = "Obsolescence", title = "Old package", description = "Package outdated", filePath = "project.csproj" }
            }
        }), TimeSpan.FromSeconds(5));
        await jobRepo.AddAsync(job1, CancellationToken.None);

        var job2 = new AnalysisJob(request.Id, AnalysisType.Security);
        job2.Start();
        job2.Complete(JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new { severity = "Critical", category = "Security", title = "CVE-2024-1234", description = "Known vulnerability", filePath = "project.csproj" }
            }
        }), TimeSpan.FromSeconds(5));
        await jobRepo.AddAsync(job2, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await sut.ConsolidateAsync(request.Id, CancellationToken.None);

        // Assert
        var findings = await findingRepo.GetByRequestIdAsync(request.Id, CancellationToken.None);
        findings.Should().HaveCount(2);
        findings.Should().Contain(f => f.Severity == Severity.Critical);
        findings.Should().Contain(f => f.Severity == Severity.High);
    }
}

