using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Services;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.API.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly JwtOptions _jwtOptions;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtOptions = new JwtOptions
        {
            Secret = "test-secret-key-with-minimum-32-characters-long",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        _sut = new AuthService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            Options.Create(_jwtOptions));
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.RegisterAsync(request, cancellationToken);

        // Assert
        result.Should().NotBeEmpty();
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), cancellationToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123"
        };
        var cancellationToken = CancellationToken.None;
        var existingUser = new User("existing@example.com", "hashedpassword");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email, cancellationToken))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var action = async () => await _sut.RegisterAsync(request, cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already registered.");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), cancellationToken), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123"
        };
        var cancellationToken = CancellationToken.None;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Email, passwordHash);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email, cancellationToken))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var action = async () => await _sut.LoginAsync(request, cancellationToken);
        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };
        var cancellationToken = CancellationToken.None;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123");
        var user = new User(request.Email, passwordHash);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email, cancellationToken))
            .ReturnsAsync(user);

        // Act & Assert
        var action = async () => await _sut.LoginAsync(request, cancellationToken);
        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task RevokeAsync_ShouldAddTokenToRevokedList()
    {
        // Arrange
        var token = "sample-jwt-token";
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.RevokeAsync(token, cancellationToken);
        var isRevoked = _sut.IsTokenRevoked(token);

        // Assert
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsTokenRevoked_WithNonRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "non-revoked-token";

        // Act
        var result = _sut.IsTokenRevoked(token);

        // Assert
        result.Should().BeFalse();
    }
}
