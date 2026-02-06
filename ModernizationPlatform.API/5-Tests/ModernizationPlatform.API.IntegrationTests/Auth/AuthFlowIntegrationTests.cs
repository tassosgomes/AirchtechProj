using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Infra.Persistence;

namespace ModernizationPlatform.API.IntegrationTests.Auth;

public class AuthFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });

                // Build the service provider and create the database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AuthFlow_RegisterLoginAndRevokeToken_ShouldWorkCorrectly()
    {
        // 1. Register a new user
        var registerRequest = new RegisterRequest
        {
            Email = "testuser@example.com",
            Password = "TestPassword123"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();
        userId.Should().NotBeEmpty();

        // 2. Login with the registered user
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrEmpty();
        loginResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // 3. Access protected endpoint with valid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        var revokeResponse = await _client.PostAsync("/api/v1/auth/revoke", null);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Try to revoke again with the same (now revoked) token - should fail
        var revokeAgainResponse = await _client.PostAsync("/api/v1/auth/revoke", null);
        revokeAgainResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123"
        };

        // Act - First registration
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Second registration with same email
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturn400()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "short"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
