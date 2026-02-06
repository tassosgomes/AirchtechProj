using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Domain.Services;

namespace ModernizationPlatform.Infra.Discovery;

public class DotNetProjectAnalyzer : IDotNetProjectAnalyzer
{
    private readonly ILogger<DotNetProjectAnalyzer> _logger;

    public DotNetProjectAnalyzer(ILogger<DotNetProjectAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(List<FrameworkInfo> Frameworks, List<DependencyInfo> Dependencies)> AnalyzeAsync(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be empty", nameof(repositoryPath));
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
        }

        _logger.LogInformation("Analyzing .NET projects in {Path}", repositoryPath);

        var frameworks = new HashSet<FrameworkInfo>();
        var dependencies = new Dictionary<string, DependencyInfo>();

        await Task.Run(() =>
        {
            var csprojFiles = Directory.EnumerateFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var csprojFile in csprojFiles)
            {
                try
                {
                    AnalyzeCsprojFile(csprojFile, frameworks, dependencies);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze project file {File}", csprojFile);
                }
            }

            // Also check for packages.config (legacy NuGet format)
            var packagesConfigFiles = Directory.EnumerateFiles(repositoryPath, "packages.config", SearchOption.AllDirectories);

            foreach (var packagesFile in packagesConfigFiles)
            {
                try
                {
                    AnalyzePackagesConfig(packagesFile, dependencies);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze packages.config {File}", packagesFile);
                }
            }
        });

        _logger.LogInformation("Found {FrameworkCount} frameworks and {DependencyCount} dependencies",
            frameworks.Count, dependencies.Count);

        return (frameworks.ToList(), dependencies.Values.ToList());
    }

    private void AnalyzeCsprojFile(
        string csprojPath,
        HashSet<FrameworkInfo> frameworks,
        Dictionary<string, DependencyInfo> dependencies)
    {
        var doc = XDocument.Load(csprojPath);
        var root = doc.Root;

        if (root == null)
        {
            return;
        }

        // Extract target framework
        var targetFramework = root.Descendants("TargetFramework").FirstOrDefault()?.Value;
        var targetFrameworks = root.Descendants("TargetFrameworks").FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            AddFramework(targetFramework, root, frameworks);
        }
        else if (!string.IsNullOrWhiteSpace(targetFrameworks))
        {
            foreach (var tf in targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                AddFramework(tf.Trim(), root, frameworks);
            }
        }

        // Extract package references
        var packageReferences = root.Descendants("PackageReference");

        foreach (var packageRef in packageReferences)
        {
            var packageName = packageRef.Attribute("Include")?.Value;
            var version = packageRef.Attribute("Version")?.Value 
                         ?? packageRef.Element("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(packageName))
            {
                // Use the latest version if package appears multiple times
                if (!dependencies.ContainsKey(packageName) || 
                    !string.IsNullOrWhiteSpace(version))
                {
                    dependencies[packageName] = new DependencyInfo(packageName, version, "NuGet");
                }
            }
        }

        // Extract project references
        var projectReferences = root.Descendants("ProjectReference");

        foreach (var projRef in projectReferences)
        {
            var projectPath = projRef.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                if (!string.IsNullOrWhiteSpace(projectName) && !dependencies.ContainsKey(projectName))
                {
                    dependencies[projectName] = new DependencyInfo(projectName, null, "ProjectReference");
                }
            }
        }
    }

    private void AddFramework(string targetFramework, XElement root, HashSet<FrameworkInfo> frameworks)
    {
        var sdk = root.Attribute("Sdk")?.Value ?? "Microsoft.NET.Sdk";
        
        var frameworkType = sdk switch
        {
            "Microsoft.NET.Sdk.Web" => "Web",
            "Microsoft.NET.Sdk.Worker" => "Worker",
            "Microsoft.NET.Sdk.BlazorWebAssembly" => "Blazor WASM",
            _ => DetermineFrameworkType(targetFramework)
        };

        var version = ExtractFrameworkVersion(targetFramework);

        frameworks.Add(new FrameworkInfo(
            Name: GetFrameworkName(targetFramework),
            Version: version,
            Type: frameworkType));
    }

    private static string GetFrameworkName(string targetFramework)
    {
        if (targetFramework.StartsWith("net") && !targetFramework.StartsWith("netstandard") && !targetFramework.StartsWith("netcoreapp"))
        {
            // net8.0, net7.0, net6.0, etc.
            return ".NET";
        }

        if (targetFramework.StartsWith("netcoreapp"))
        {
            return ".NET Core";
        }

        if (targetFramework.StartsWith("netstandard"))
        {
            return ".NET Standard";
        }

        if (targetFramework.StartsWith("net4"))
        {
            return ".NET Framework";
        }

        return targetFramework;
    }

    private static string ExtractFrameworkVersion(string targetFramework)
    {
        // net8.0 -> 8.0
        // netcoreapp3.1 -> 3.1
        // netstandard2.0 -> 2.0
        // net48 -> 4.8

        var digits = new string(targetFramework.Where(c => char.IsDigit(c) || c == '.').ToArray());

        if (digits.Length == 0)
        {
            return targetFramework;
        }

        // net48 -> 4.8, net472 -> 4.7.2
        if (targetFramework.StartsWith("net") && !targetFramework.Contains('.') && digits.Length > 2)
        {
            return $"{digits[0]}.{digits[1]}{(digits.Length > 2 ? $".{digits.Substring(2)}" : "")}";
        }

        return digits;
    }

    private static string DetermineFrameworkType(string targetFramework)
    {
        // Basic heuristic - can be improved with more analysis
        return "Library";
    }

    private void AnalyzePackagesConfig(string packagesConfigPath, Dictionary<string, DependencyInfo> dependencies)
    {
        var doc = XDocument.Load(packagesConfigPath);
        var packages = doc.Descendants("package");

        foreach (var package in packages)
        {
            var id = package.Attribute("id")?.Value;
            var version = package.Attribute("version")?.Value;

            if (!string.IsNullOrWhiteSpace(id) && !dependencies.ContainsKey(id))
            {
                dependencies[id] = new DependencyInfo(id, version, "NuGet");
            }
        }
    }
}
