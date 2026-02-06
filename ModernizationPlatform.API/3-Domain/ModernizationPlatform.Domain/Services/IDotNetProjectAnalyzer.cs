namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Service responsible for analyzing .NET projects and detecting frameworks/dependencies
/// </summary>
public interface IDotNetProjectAnalyzer
{
    /// <summary>
    /// Analyzes .NET projects in the repository
    /// </summary>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <returns>Detected frameworks and dependencies</returns>
    Task<(List<FrameworkInfo> Frameworks, List<DependencyInfo> Dependencies)> AnalyzeAsync(string repositoryPath);
}
