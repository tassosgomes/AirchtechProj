using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Infra.Messaging.Connection;
using ModernizationPlatform.Infra.Persistence;
using RabbitMQ.Client;

namespace ModernizationPlatform.API.IntegrationTests.Prompts;

// Fake implementations for testing (no-op)
public class FakeRabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    public IConnection GetConnection() => null!; // Never called in tests
}

public class FakeJobPublisher : IJobPublisher
{
    public Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class FakeResultConsumer : IResultConsumer
{
    public Task StartConsumingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class PromptCatalogIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _databaseName = "TestDb_Prompts_" + Guid.NewGuid();

    public PromptCatalogIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // Remove RabbitMQ hosted services to avoid connection errors in tests
                var hostedServices = services.Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                // Replace RabbitMQ services with fake implementations
                var rabbitMqConnection = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqConnectionProvider));
                if (rabbitMqConnection != null) services.Remove(rabbitMqConnection);
                services.AddSingleton<IRabbitMqConnectionProvider, FakeRabbitMqConnectionProvider>();

                var jobPublisher = services.SingleOrDefault(d => d.ServiceType == typeof(IJobPublisher));
                if (jobPublisher != null) services.Remove(jobPublisher);
                services.AddSingleton<IJobPublisher, FakeJobPublisher>();

                var resultConsumer = services.SingleOrDefault(d => d.ServiceType == typeof(IResultConsumer));
                if (resultConsumer != null) services.Remove(resultConsumer);
                services.AddSingleton<IResultConsumer, FakeResultConsumer>();

                // Build and create database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task PromptCatalog_CompleteFlow_ShouldWorkCorrectly()
    {
        // 1. Register and login to get authentication token
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Create a new prompt
        var createRequest = new CreatePromptRequest(
            AnalysisType: AnalysisType.Security,
            Content: "Analyze security vulnerabilities in the codebase"
        );

        var createResponse = await client.PostAsJsonAsync("/api/v1/prompts", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPrompt = await createResponse.Content.ReadFromJsonAsync<PromptResponse>();
        createdPrompt.Should().NotBeNull();
        createdPrompt!.AnalysisType.Should().Be(AnalysisType.Security);
        createdPrompt.Content.Should().Be(createRequest.Content);
        createdPrompt.Id.Should().NotBeEmpty();

        // 3. Get all prompts - should contain the created prompt
        var getAllResponse = await client.GetAsync("/api/v1/prompts");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allPrompts = await getAllResponse.Content.ReadFromJsonAsync<List<PromptResponse>>();
        allPrompts.Should().NotBeNull();
        allPrompts.Should().HaveCount(1);
        allPrompts![0].Id.Should().Be(createdPrompt.Id);

        // 4. Get prompt by ID
        var getByIdResponse = await client.GetAsync($"/api/v1/prompts/{createdPrompt.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedPrompt = await getByIdResponse.Content.ReadFromJsonAsync<PromptResponse>();
        retrievedPrompt.Should().NotBeNull();
        retrievedPrompt!.Id.Should().Be(createdPrompt.Id);

        // 5. Update the prompt
        var updateRequest = new UpdatePromptRequest(
            Content: "Updated security analysis prompt with more details"
        );

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/prompts/{createdPrompt.Id}", updateRequest);
        if (!updateResponse.IsSuccessStatusCode)
        {
            var errorContent = await updateResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Update failed with status {updateResponse.StatusCode}: {errorContent}");
        }
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPrompt = await updateResponse.Content.ReadFromJsonAsync<PromptResponse>();
        updatedPrompt.Should().NotBeNull();
        updatedPrompt!.Content.Should().Be(updateRequest.Content);
        updatedPrompt.UpdatedAt.Should().BeAfter(createdPrompt.UpdatedAt);

        // 6. Create another prompt with different analysis type
        var createRequest2 = new CreatePromptRequest(
            AnalysisType: AnalysisType.Obsolescence,
            Content: "Analyze obsolete dependencies and technologies"
        );

        var createResponse2 = await client.PostAsJsonAsync("/api/v1/prompts", createRequest2);
        createResponse2.StatusCode.Should().Be(HttpStatusCode.Created);

        // 7. Get all prompts - should now have 2 prompts
        var getAllResponse2 = await client.GetAsync("/api/v1/prompts");
        getAllResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var allPrompts2 = await getAllResponse2.Content.ReadFromJsonAsync<List<PromptResponse>>();
        allPrompts2.Should().NotBeNull();
        allPrompts2.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreatePrompt_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var createRequest = new CreatePromptRequest(
            AnalysisType: AnalysisType.Documentation,
            Content: "Documentation prompt"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/prompts", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPromptById_NonExisting_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/prompts/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePrompt_NonExisting_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistingId = Guid.NewGuid();
        var updateRequest = new UpdatePromptRequest(Content: "Updated content");

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/prompts/{nonExistingId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePrompt_WithEmptyContent_ShouldReturn400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetAuthenticationToken(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createRequest = new CreatePromptRequest(
            AnalysisType: AnalysisType.Observability,
            Content: ""
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/prompts", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> GetAuthenticationToken(HttpClient client)
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
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }
}
