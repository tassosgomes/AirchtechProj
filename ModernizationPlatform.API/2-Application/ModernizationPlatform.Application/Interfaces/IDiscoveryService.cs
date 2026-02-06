using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for analyzing a repository and generating the SharedContext
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Executes the discovery phase: clone repository, analyze structure, detect languages/frameworks/dependencies
    /// </summary>
    /// <param name="request">The analysis request containing repository URL, provider and access token</param>
    /// <param name="accessToken">Optional access token for private repositories (never persisted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated SharedContext</returns>
    Task<SharedContext> ExecuteDiscoveryAsync(
        AnalysisRequest request, 
        string? accessToken, 
        CancellationToken cancellationToken);
}
