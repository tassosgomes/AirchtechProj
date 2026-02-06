namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Service responsible for mapping directory structure
/// </summary>
public interface IDirectoryStructureMapper
{
    /// <summary>
    /// Maps the directory structure of a repository
    /// </summary>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <returns>Directory tree structure</returns>
    DirectoryNode MapStructure(string repositoryPath);
}
