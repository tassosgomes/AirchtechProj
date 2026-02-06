namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Service responsible for detecting programming languages in a repository
/// </summary>
public interface ILanguageDetectorService
{
    /// <summary>
    /// Detects programming languages by analyzing file extensions
    /// </summary>
    /// <param name="repositoryPath">Path to the repository</param>
    /// <returns>List of detected languages with file and line counts</returns>
    Task<List<LanguageInfo>> DetectLanguagesAsync(string repositoryPath);
}
