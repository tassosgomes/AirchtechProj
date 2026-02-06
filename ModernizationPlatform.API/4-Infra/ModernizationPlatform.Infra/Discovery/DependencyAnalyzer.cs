using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Domain.Services;

namespace ModernizationPlatform.Infra.Discovery;

public class DependencyAnalyzer : IDependencyAnalyzer
{
    private readonly ILogger<DependencyAnalyzer> _logger;

    public DependencyAnalyzer(ILogger<DependencyAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<DependencyInfo>> AnalyzeDependenciesAsync(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be empty", nameof(repositoryPath));
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
        }

        _logger.LogInformation("Analyzing dependencies in {Path}", repositoryPath);

        var dependencies = new Dictionary<string, DependencyInfo>();

        await Task.Run(() =>
        {
            // npm/yarn (JavaScript/TypeScript)
            AnalyzePackageJson(repositoryPath, dependencies);

            // Maven (Java)
            AnalyzePomXml(repositoryPath, dependencies);

            // Gradle (Java/Kotlin)
            AnalyzeBuildGradle(repositoryPath, dependencies);

            // pip (Python)
            AnalyzeRequirementsTxt(repositoryPath, dependencies);

            // Gemfile (Ruby)
            AnalyzeGemfile(repositoryPath, dependencies);

            // Go modules
            AnalyzeGoMod(repositoryPath, dependencies);
        });

        _logger.LogInformation("Found {Count} dependencies from various package managers", dependencies.Count);

        return dependencies.Values.ToList();
    }

    private void AnalyzePackageJson(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var packageJsonFiles = Directory.EnumerateFiles(repositoryPath, "package.json", SearchOption.AllDirectories);

        foreach (var packageJsonPath in packageJsonFiles)
        {
            try
            {
                var json = File.ReadAllText(packageJsonPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                ExtractNpmDependencies(root, "dependencies", dependencies);
                ExtractNpmDependencies(root, "devDependencies", dependencies);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse package.json at {Path}", packageJsonPath);
            }
        }
    }

    private void ExtractNpmDependencies(JsonElement root, string propertyName, Dictionary<string, DependencyInfo> dependencies)
    {
        if (root.TryGetProperty(propertyName, out var depsElement) && depsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var dep in depsElement.EnumerateObject())
            {
                var version = dep.Value.GetString()?.TrimStart('^', '~', '>', '<', '=');
                
                if (!dependencies.ContainsKey(dep.Name))
                {
                    dependencies[dep.Name] = new DependencyInfo(dep.Name, version, "npm");
                }
            }
        }
    }

    private void AnalyzePomXml(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var pomFiles = Directory.EnumerateFiles(repositoryPath, "pom.xml", SearchOption.AllDirectories);

        foreach (var pomPath in pomFiles)
        {
            try
            {
                var doc = XDocument.Load(pomPath);
                var ns = doc.Root?.GetDefaultNamespace();

                if (ns == null)
                {
                    continue;
                }

                var dependencyElements = doc.Descendants(ns + "dependency");

                foreach (var dep in dependencyElements)
                {
                    var groupId = dep.Element(ns + "groupId")?.Value;
                    var artifactId = dep.Element(ns + "artifactId")?.Value;
                    var version = dep.Element(ns + "version")?.Value;

                    if (!string.IsNullOrWhiteSpace(artifactId))
                    {
                        var fullName = string.IsNullOrWhiteSpace(groupId) 
                            ? artifactId 
                            : $"{groupId}:{artifactId}";

                        if (!dependencies.ContainsKey(fullName))
                        {
                            dependencies[fullName] = new DependencyInfo(fullName, version, "Maven");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse pom.xml at {Path}", pomPath);
            }
        }
    }

    private void AnalyzeBuildGradle(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var gradleFiles = Directory.EnumerateFiles(repositoryPath, "build.gradle*", SearchOption.AllDirectories);

        foreach (var gradlePath in gradleFiles)
        {
            try
            {
                var content = File.ReadAllText(gradlePath);
                
                // Simple regex-based parsing for common dependency declarations
                // implementation 'group:artifact:version'
                // compile 'group:artifact:version'
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    content,
                    @"(?:implementation|compile|api|testImplementation)\s+['""]([^:]+):([^:]+):([^'""]+)['""]");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count >= 4)
                    {
                        var groupId = match.Groups[1].Value;
                        var artifactId = match.Groups[2].Value;
                        var version = match.Groups[3].Value;

                        var fullName = $"{groupId}:{artifactId}";

                        if (!dependencies.ContainsKey(fullName))
                        {
                            dependencies[fullName] = new DependencyInfo(fullName, version, "Gradle");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse build.gradle at {Path}", gradlePath);
            }
        }
    }

    private void AnalyzeRequirementsTxt(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var requirementsFiles = Directory.EnumerateFiles(repositoryPath, "requirements*.txt", SearchOption.AllDirectories);

        foreach (var reqPath in requirementsFiles)
        {
            try
            {
                var lines = File.ReadAllLines(reqPath);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    // Parse package==version or package>=version
                    var parts = trimmed.Split(new[] { "==", ">=", "<=", "~=", "!=" }, StringSplitOptions.None);
                    
                    if (parts.Length > 0)
                    {
                        var packageName = parts[0].Trim();
                        var version = parts.Length > 1 ? parts[1].Trim() : null;

                        if (!string.IsNullOrWhiteSpace(packageName) && !dependencies.ContainsKey(packageName))
                        {
                            dependencies[packageName] = new DependencyInfo(packageName, version, "pip");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse requirements.txt at {Path}", reqPath);
            }
        }
    }

    private void AnalyzeGemfile(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var gemfiles = Directory.EnumerateFiles(repositoryPath, "Gemfile", SearchOption.AllDirectories);

        foreach (var gemfilePath in gemfiles)
        {
            try
            {
                var lines = File.ReadAllLines(gemfilePath);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    // gem 'gem_name', 'version'
                    var match = System.Text.RegularExpressions.Regex.Match(
                        trimmed,
                        @"gem\s+['""]([^'""]+)['""](?:\s*,\s*['""]([^'""]+)['""])?");

                    if (match.Success)
                    {
                        var gemName = match.Groups[1].Value;
                        var version = match.Groups.Count > 2 ? match.Groups[2].Value : null;

                        if (!string.IsNullOrWhiteSpace(gemName) && !dependencies.ContainsKey(gemName))
                        {
                            dependencies[gemName] = new DependencyInfo(gemName, version, "RubyGems");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Gemfile at {Path}", gemfilePath);
            }
        }
    }

    private void AnalyzeGoMod(string repositoryPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var goModFiles = Directory.EnumerateFiles(repositoryPath, "go.mod", SearchOption.AllDirectories);

        foreach (var goModPath in goModFiles)
        {
            try
            {
                var lines = File.ReadAllLines(goModPath);
                var inRequireBlock = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("require ("))
                    {
                        inRequireBlock = true;
                        continue;
                    }

                    if (trimmed == ")")
                    {
                        inRequireBlock = false;
                        continue;
                    }

                    if (inRequireBlock || trimmed.StartsWith("require "))
                    {
                        var parts = trimmed.Replace("require ", "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        if (parts.Length >= 2)
                        {
                            var moduleName = parts[0];
                            var version = parts[1];

                            if (!dependencies.ContainsKey(moduleName))
                            {
                                dependencies[moduleName] = new DependencyInfo(moduleName, version, "Go");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse go.mod at {Path}", goModPath);
            }
        }
    }
}
