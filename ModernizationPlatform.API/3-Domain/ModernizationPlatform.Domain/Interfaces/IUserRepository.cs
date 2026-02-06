using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}
