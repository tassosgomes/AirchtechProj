using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Service responsible for cloning git repositories
/// </summary>
public interface IGitCloneService
{
    /// <summary>
    /// Clones a repository to a temporary directory
    /// </summary>
    /// <param name="repositoryUrl">Repository URL</param>
    /// <param name="provider">Git provider (GitHub or Azure DevOps)</param>
    /// <param name="accessToken">Optional access token for authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the cloned repository</returns>
    Task<string> CloneRepositoryAsync(
        string repositoryUrl,
        SourceProvider provider,
        string? accessToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up the temporary directory
    /// </summary>
    /// <param name="repositoryPath">Path to clean up</param>
    void CleanupRepository(string repositoryPath);
}
