namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IRepositorySnapshotBuilder
{
    Task<string> BuildAsync(string repositoryPath, CancellationToken cancellationToken);
}
