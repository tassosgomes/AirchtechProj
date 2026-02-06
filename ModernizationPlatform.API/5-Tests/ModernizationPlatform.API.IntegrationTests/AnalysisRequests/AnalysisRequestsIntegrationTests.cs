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
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Persistence;
using RabbitMQ.Client;

namespace ModernizationPlatform.API.IntegrationTests.AnalysisRequests;

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

public class AnalysisRequestsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = "TestDb_AnalysisRequests_" + Guid.NewGuid();

    public AnalysisRequestsIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task AnalysisRequestFlow_ShouldReturnQueuePositions()
    {
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstRequest = new CreateAnalysisRequest(
            "https://github.com/org/repo-one",
            SourceProvider.GitHub,
            "token-1",
            new List<AnalysisType> { AnalysisType.Security });

        var firstResponse = await client.PostAsJsonAsync("/api/v1/analysis-requests", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdFirst = await firstResponse.Content.ReadFromJsonAsync<AnalysisRequestResponse>(JsonOptions);
        createdFirst.Should().NotBeNull();
        createdFirst!.QueuePosition.Should().Be(1);

        await Task.Delay(5);

        var secondRequest = new CreateAnalysisRequest(
            "https://github.com/org/repo-two",
            SourceProvider.GitHub,
            "token-2",
            new List<AnalysisType> { AnalysisType.Security, AnalysisType.Observability });

        var secondResponse = await client.PostAsJsonAsync("/api/v1/analysis-requests", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSecond = await secondResponse.Content.ReadFromJsonAsync<AnalysisRequestResponse>(JsonOptions);
        createdSecond.Should().NotBeNull();
        createdSecond!.QueuePosition.Should().Be(2);

        var statusResponse = await client.GetAsync($"/api/v1/analysis-requests/{createdSecond.Id}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<AnalysisRequestResponse>(JsonOptions);
        status.Should().NotBeNull();
        status!.QueuePosition.Should().Be(2);
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
