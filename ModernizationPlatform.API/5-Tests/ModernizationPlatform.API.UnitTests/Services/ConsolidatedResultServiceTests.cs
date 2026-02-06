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

public sealed class ConsolidatedResultServiceTests
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ConsolidatedResultService>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }

    [Fact]
    public async Task GetConsolidatedResultAsync_RequestNotFound_ReturnsNull()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidatedResultService>();

        var requestId = Guid.NewGuid();

        // Act
        var result = await sut.GetConsolidatedResultAsync(requestId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConsolidatedResultAsync_WithFindings_ReturnsConsolidatedResult()
    {
        // Arrange
        var dbName = "Test_" + Guid.NewGuid();
        var provider = BuildProvider(dbName);
        await using var scope = provider.CreateAsyncScope();
        
        var requestRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var findingRepo = scope.ServiceProvider.GetRequiredService<IFindingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sut = scope.ServiceProvider.GetRequiredService<ConsolidatedResultService>();

        var request = new AnalysisRequest("https://github.com/test/repo", SourceProvider.GitHub, [AnalysisType.Obsolescence]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        request.Complete();
        await requestRepo.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Criar jobs para associar os findings
        var job = new AnalysisJob(request.Id, AnalysisType.Obsolescence);
        await jobRepo.AddAsync(job, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var findings = new List<Finding>
        {
            new(job.Id, Severity.Critical, "Security", "CVE", "Known vulnerability", "file1.cs"),
            new(job.Id, Severity.High, "Obsolescence", "Old package", "Package is outdated", "file2.cs"),
            new(job.Id, Severity.Medium, "Observability", "No logging", "Missing logs", "file3.cs")
        };

        foreach (var finding in findings)
        {
            await findingRepo.AddAsync(finding, CancellationToken.None);
        }
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await sut.GetConsolidatedResultAsync(request.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RequestId.Should().Be(request.Id);
        result.RepositoryUrl.Should().Be("https://github.com/test/repo");
        result.Summary.TotalFindings.Should().Be(3);
        result.Findings.Should().HaveCount(3);
    }
}
