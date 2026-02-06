using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Persistence;
using Xunit;
using FluentAssertions;

namespace ModernizationPlatform.API.IntegrationTests.Consolidation;

public class ConsolidationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = "TestDb_Consolidation_" + Guid.NewGuid();

    public ConsolidationIntegrationTests(WebApplicationFactory<Program> factory)
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

                // Remove hosted services para não interferir
                var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task ConsolidateAsync_WithMultipleJobs_CreatesFindings()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var consolidationService = scope.ServiceProvider.GetRequiredService<IConsolidationService>();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var findingRepository = scope.ServiceProvider.GetRequiredService<IFindingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Criar request
        var request = new AnalysisRequest("https://github.com/test/repo", SourceProvider.GitHub, 
            [AnalysisType.Obsolescence, AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepository.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Criar jobs com outputs
        var job1 = new AnalysisJob(request.Id, AnalysisType.Obsolescence);
        job1.Start();
        var output1 = JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new
                {
                    severity = "High",
                    category = "Obsolescence",
                    title = "Outdated package",
                    description = "Package X is outdated",
                    filePath = "src/project.csproj"
                },
                new
                {
                    severity = "Medium",
                    category = "Obsolescence",
                    title = "Legacy API",
                    description = "Using deprecated API",
                    filePath = "src/Service.cs"
                }
            }
        });
        job1.Complete(output1, TimeSpan.FromSeconds(10));
        await jobRepository.AddAsync(job1, CancellationToken.None);

        var job2 = new AnalysisJob(request.Id, AnalysisType.Security);
        job2.Start();
        var output2 = JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new
                {
                    severity = "Critical",
                    category = "Security",
                    title = "CVE-2024-1234",
                    description = "Known vulnerability in dependency",
                    filePath = "src/project.csproj"
                }
            }
        });
        job2.Complete(output2, TimeSpan.FromSeconds(8));
        await jobRepository.AddAsync(job2, CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await consolidationService.ConsolidateAsync(request.Id, CancellationToken.None);

        // Assert
        var findings = await findingRepository.GetByRequestIdAsync(request.Id, CancellationToken.None);
        findings.Should().HaveCount(3);
        findings.Should().Contain(f => f.Severity == Severity.Critical && f.Category == "Security");
        findings.Should().Contain(f => f.Severity == Severity.High && f.Category == "Obsolescence");
        findings.Should().Contain(f => f.Severity == Severity.Medium && f.Category == "Obsolescence");

        var updatedRequest = await requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        updatedRequest.Should().NotBeNull();
        updatedRequest!.Status.Should().Be(RequestStatus.Completed);
    }

    [Fact]
    public async Task ConsolidateAsync_WithDifferentOutputFormats_ParsesCorrectly()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var consolidationService = scope.ServiceProvider.GetRequiredService<IConsolidationService>();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var findingRepository = scope.ServiceProvider.GetRequiredService<IFindingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = new AnalysisRequest("https://github.com/test/repo2", SourceProvider.GitHub, 
            [AnalysisType.Observability, AnalysisType.Documentation]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepository.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Job com formato "issues"
        var job1 = new AnalysisJob(request.Id, AnalysisType.Observability);
        job1.Start();
        var output1 = JsonSerializer.Serialize(new
        {
            issues = new[]
            {
                new
                {
                    severity = "Low",
                    category = "Observability",
                    title = "Missing metrics",
                    description = "No metrics endpoint",
                    file = "src/Startup.cs"
                }
            }
        });
        job1.Complete(output1, TimeSpan.FromSeconds(5));
        await jobRepository.AddAsync(job1, CancellationToken.None);

        // Job com formato de array direto
        var job2 = new AnalysisJob(request.Id, AnalysisType.Documentation);
        job2.Start();
        var output2 = JsonSerializer.Serialize(new[]
        {
            new
            {
                severity = "Informative",
                category = "Documentation",
                title = "Missing README",
                description = "No README file found",
                path = "README.md"
            }
        });
        job2.Complete(output2, TimeSpan.FromSeconds(3));
        await jobRepository.AddAsync(job2, CancellationToken.None);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await consolidationService.ConsolidateAsync(request.Id, CancellationToken.None);

        // Assert
        var findings = await findingRepository.GetByRequestIdAsync(request.Id, CancellationToken.None);
        findings.Should().HaveCount(2);
        findings.Should().Contain(f => f.Severity == Severity.Low && f.FilePath == "src/Startup.cs");
        findings.Should().Contain(f => f.Severity == Severity.Informative && f.FilePath == "README.md");
    }
}
