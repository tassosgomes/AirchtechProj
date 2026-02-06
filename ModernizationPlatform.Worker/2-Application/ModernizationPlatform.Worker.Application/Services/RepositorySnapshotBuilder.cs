using System.Text;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Application.Services;

public sealed class RepositorySnapshotBuilder : IRepositorySnapshotBuilder
{
    private const int MaxFiles = 200;
    private const int MaxFileSizeBytes = 200_000;
    private const int MaxTotalBytes = 1_000_000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".csproj",
        ".cshtml",
        ".json",
        ".yml",
        ".yaml",
        ".xml",
        ".md",
        ".txt",
        ".js",
        ".ts",
        ".tsx",
        ".jsx",
        ".html",
        ".css",
        ".scss",
        ".ps1",
        ".sh",
        ".sql"
    };

    private static readonly string[] SkippedDirectories =
    [
        "bin",
        "obj",
        ".git",
        "node_modules",
        ".idea",
        ".vscode"
    ];

    public async Task<string> BuildAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath) || !Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException("Repository path not found");
        }

        var builder = new StringBuilder();
        builder.AppendLine("Repository snapshot (truncated)");

        var totalBytes = 0;
        var filesIncluded = 0;

        foreach (var file in Directory.EnumerateFiles(repositoryPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ShouldSkip(file, repositoryPath))
            {
                continue;
            }

            var extension = Path.GetExtension(file);
            if (!AllowedExtensions.Contains(extension))
            {
                continue;
            }

            var fileInfo = new FileInfo(file);
            if (fileInfo.Length == 0 || fileInfo.Length > MaxFileSizeBytes)
            {
                continue;
            }

            if (filesIncluded >= MaxFiles || totalBytes + fileInfo.Length > MaxTotalBytes)
            {
                break;
            }

            var relativePath = Path.GetRelativePath(repositoryPath, file);
            var content = await File.ReadAllTextAsync(file, cancellationToken);

            builder.AppendLine($"--- {relativePath} ---");
            builder.AppendLine(content);
            builder.AppendLine();

            totalBytes += (int)fileInfo.Length;
            filesIncluded++;
        }

        if (filesIncluded == 0)
        {
            builder.AppendLine("No source files captured.");
        }

        return builder.ToString();
    }

    private static bool ShouldSkip(string filePath, string repositoryPath)
    {
        var relative = Path.GetRelativePath(repositoryPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var segment in segments)
        {
            if (SkippedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
