namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IGitCloneService
{
    Task<string> CloneRepositoryAsync(
        string repositoryUrl,
        string provider,
        string? accessToken,
        CancellationToken cancellationToken);

    void CleanupRepository(string repositoryPath);
}
