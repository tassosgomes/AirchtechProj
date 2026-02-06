using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Interfaces;

public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task RevokeAsync(string token, CancellationToken cancellationToken);
    bool IsTokenRevoked(string token);
}
