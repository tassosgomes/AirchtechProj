using Microsoft.Extensions.Logging;
using ModernizationPlatform.Domain.Services;

namespace ModernizationPlatform.Infra.Discovery;

public class LanguageDetectorService : ILanguageDetectorService
{
    private readonly ILogger<LanguageDetectorService> _logger;

    private static readonly Dictionary<string, string> ExtensionToLanguage = new()
    {
        { ".cs", "C#" },
        { ".csproj", ".NET" },
        { ".sln", ".NET" },
        { ".js", "JavaScript" },
        { ".jsx", "JavaScript" },
        { ".ts", "TypeScript" },
        { ".tsx", "TypeScript" },
        { ".py", "Python" },
        { ".java", "Java" },
        { ".go", "Go" },
        { ".rb", "Ruby" },
        { ".php", "PHP" },
        { ".cpp", "C++" },
        { ".cc", "C++" },
        { ".c", "C" },
        { ".h", "C/C++" },
        { ".rs", "Rust" },
        { ".kt", "Kotlin" },
        { ".swift", "Swift" },
        { ".sql", "SQL" },
        { ".scala", "Scala" },
        { ".clj", "Clojure" },
        { ".ex", "Elixir" },
        { ".exs", "Elixir" },
        { ".dart", "Dart" },
        { ".r", "R" },
        { ".m", "Objective-C" },
        { ".pl", "Perl" },
        { ".sh", "Shell" },
        { ".ps1", "PowerShell" },
        { ".html", "HTML" },
        { ".css", "CSS" },
        { ".scss", "SCSS" },
        { ".xml", "XML" },
        { ".json", "JSON" },
        { ".yaml", "YAML" },
        { ".yml", "YAML" }
    };

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", "bin", "obj", ".git", ".vs", ".vscode", "packages",
        "dist", "build", "target", "__pycache__", ".gradle", ".idea"
    };

    public LanguageDetectorService(ILogger<LanguageDetectorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<LanguageInfo>> DetectLanguagesAsync(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be empty", nameof(repositoryPath));
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
        }

        _logger.LogInformation("Detecting languages in {Path}", repositoryPath);

        var languageStats = new Dictionary<string, (int FileCount, int LineCount)>();

        await Task.Run(() =>
        {
            var files = Directory.EnumerateFiles(repositoryPath, "*.*", SearchOption.AllDirectories)
                .Where(file => !IsExcludedPath(file, repositoryPath));

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                
                if (ExtensionToLanguage.TryGetValue(extension, out var language))
                {
                    var lineCount = CountLines(file);
                    
                    if (languageStats.TryGetValue(language, out var stats))
                    {
                        languageStats[language] = (stats.FileCount + 1, stats.LineCount + lineCount);
                    }
                    else
                    {
                        languageStats[language] = (1, lineCount);
                    }
                }
            }
        });

        var result = languageStats
            .Select(kvp => new LanguageInfo(kvp.Key, kvp.Value.FileCount, kvp.Value.LineCount))
            .OrderByDescending(l => l.LineCount)
            .ToList();

        _logger.LogInformation("Detected {Count} languages", result.Count);

        return result;
    }

    private static bool IsExcludedPath(string filePath, string repositoryPath)
    {
        var relativePath = Path.GetRelativePath(repositoryPath, filePath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return pathParts.Any(part => ExcludedDirectories.Contains(part));
    }

    private static int CountLines(string filePath)
    {
        try
        {
            // Simple line count - don't read very large files
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 10 * 1024 * 1024) // Skip files > 10MB
            {
                return 0;
            }

            return File.ReadLines(filePath).Count();
        }
        catch
        {
            // Ignore files that can't be read
            return 0;
        }
    }
}
