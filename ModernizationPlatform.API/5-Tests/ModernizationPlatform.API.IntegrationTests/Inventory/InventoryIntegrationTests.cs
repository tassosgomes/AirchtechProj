using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Persistence;
using RabbitMQ.Client;

namespace ModernizationPlatform.API.IntegrationTests.Inventory;

public class FakeRabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    public IConnection GetConnection() => null!;
}

public class FakeJobPublisher : IJobPublisher
{
    public Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class FakeResultConsumer : IResultConsumer
{
    public Task StartConsumingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class InventoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = "TestDb_Inventory_" + Guid.NewGuid();

    public InventoryIntegrationTests(WebApplicationFactory<Program> factory)
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

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task InventoryFlow_ShouldReturnRepositoriesFindingsAndTimeline()
    {
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await SeedInventoryDataAsync();

        var technology = Uri.EscapeDataString("C#");
        var repositoriesResponse = await client.GetAsync($"/api/v1/inventory/repositories?technology={technology}&_page=1&_size=10");
        repositoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var repositoriesResult = await repositoriesResponse.Content.ReadFromJsonAsync<PagedResult<RepositorySummary>>(JsonOptions);
        repositoriesResult.Should().NotBeNull();
        repositoriesResult!.Data.Should().HaveCount(1);

        var repositoryId = repositoriesResult.Data[0].Id;

        var timelineResponse = await client.GetAsync($"/api/v1/inventory/repositories/{repositoryId}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<RepositoryTimeline>(JsonOptions);
        timeline.Should().NotBeNull();
        timeline!.Analyses.Should().HaveCount(1);
        timeline.Analyses[0].Summary[Severity.High.ToString()].Should().Be(1);

        var findingsResponse = await client.GetAsync("/api/v1/inventory/findings?severity=High&_page=1&_size=10");
        findingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var findings = await findingsResponse.Content.ReadFromJsonAsync<PagedResult<FindingSummary>>(JsonOptions);
        findings.Should().NotBeNull();
        findings!.Data.Should().HaveCount(1);
        findings.Data[0].Severity.Should().Be(Severity.High.ToString());
    }

    private async Task SeedInventoryDataAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var consolidationService = scope.ServiceProvider.GetRequiredService<IConsolidationService>();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var sharedContextRepository = scope.ServiceProvider.GetRequiredService<IRepository<SharedContext>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = new AnalysisRequest("https://github.com/org/inventory-repo", SourceProvider.GitHub,
            [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        await requestRepository.AddAsync(request, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var sharedContext = new SharedContext(
            request.Id,
            1,
            new[] { "C#" },
            new[] { "ASP.NET Core 8 (Framework)" },
            new[] { "Newtonsoft.Json@13.0 (NuGet)" },
            "{}");
        await sharedContextRepository.AddAsync(sharedContext, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var job = new AnalysisJob(request.Id, AnalysisType.Security);
        job.Start();
        job.Complete(JsonSerializer.Serialize(new
        {
            findings = new[]
            {
                new
                {
                    severity = "High",
                    category = "Security",
                    title = "CVE-2025-0001",
                    description = "Known vulnerability",
                    filePath = "src/app.cs"
                }
            }
        }), TimeSpan.FromSeconds(5));
        await jobRepository.AddAsync(job, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await consolidationService.ConsolidateAsync(request.Id, CancellationToken.None);
    }

    private static async Task<string> GetAuthenticationToken(HttpClient client)
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"testuser_{Guid.NewGuid()}@example.com",
            Password = "TestPassword123"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed with status {registerResponse.StatusCode}: {errorContent}");
        }

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {loginResponse.StatusCode}: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return loginResult!.Token;
    }
}
