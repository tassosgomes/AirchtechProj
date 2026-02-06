namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Service responsible for analyzing dependencies from various package managers
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// Analyzes dependencies from package.json, pom.xml, requirements.txt, etc.
    /// </summary>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <returns>List of detected dependencies</returns>
    Task<List<DependencyInfo>> AnalyzeDependenciesAsync(string repositoryPath);
}
