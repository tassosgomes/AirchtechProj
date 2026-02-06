using Microsoft.Extensions.Logging;
using ModernizationPlatform.Domain.Services;

namespace ModernizationPlatform.Infra.Discovery;

public class DirectoryStructureMapper : IDirectoryStructureMapper
{
    private readonly ILogger<DirectoryStructureMapper> _logger;

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", "bin", "obj", ".git", ".vs", ".vscode", "packages",
        "dist", "build", "target", "__pycache__", ".gradle", ".idea", ".next",
        "coverage", "logs", "temp", "tmp"
    };

    private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe", ".pdb", ".cache", ".log", ".lock", ".suo", ".user"
    };

    public DirectoryStructureMapper(ILogger<DirectoryStructureMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DirectoryNode MapStructure(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be empty", nameof(repositoryPath));
        }

        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
        }

        _logger.LogInformation("Mapping directory structure of {Path}", repositoryPath);

        var rootDirInfo = new DirectoryInfo(repositoryPath);
        var result = MapDirectory(rootDirInfo, repositoryPath);

        _logger.LogInformation("Directory structure mapped successfully");

        return result;
    }

    private DirectoryNode MapDirectory(DirectoryInfo directory, string rootPath)
    {
        var node = new DirectoryNode
        {
            Name = directory.Name,
            Type = "directory",
            Children = []
        };

        try
        {
            // Add subdirectories
            foreach (var subDir in directory.GetDirectories())
            {
                if (ShouldIncludeDirectory(subDir.Name))
                {
                    var childNode = MapDirectory(subDir, rootPath);
                    node.Children.Add(childNode);
                }
            }

            // Add files
            foreach (var file in directory.GetFiles())
            {
                if (ShouldIncludeFile(file))
                {
                    var fileNode = new DirectoryNode
                    {
                        Name = file.Name,
                        Type = "file",
                        Size = file.Length,
                        Children = []
                    };
                    node.Children.Add(fileNode);
                }
            }

            // Sort children: directories first, then files, both alphabetically
            node = node with
            {
                Children = node.Children
                    .OrderBy(n => n.Type == "file" ? 1 : 0)
                    .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory {Path}", directory.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accessing directory {Path}", directory.FullName);
        }

        return node;
    }

    private static bool ShouldIncludeDirectory(string directoryName)
    {
        return !ExcludedDirectories.Contains(directoryName);
    }

    private static bool ShouldIncludeFile(FileInfo file)
    {
        // Exclude very large files (> 50MB)
        if (file.Length > 50 * 1024 * 1024)
        {
            return false;
        }

        var extension = file.Extension;
        return !ExcludedExtensions.Contains(extension);
    }
}
