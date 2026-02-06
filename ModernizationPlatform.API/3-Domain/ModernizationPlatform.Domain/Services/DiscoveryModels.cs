namespace ModernizationPlatform.Domain.Services;

/// <summary>
/// Represents detected language information
/// </summary>
public record LanguageInfo(string Name, int FileCount, int LineCount);

/// <summary>
/// Represents detected framework information
/// </summary>
public record FrameworkInfo(string Name, string? Version, string Type);

/// <summary>
/// Represents a dependency (NuGet, npm, Maven, etc.)
/// </summary>
public record DependencyInfo(string Name, string? Version, string Type);

/// <summary>
/// Represents a directory node in the structure tree
/// </summary>
public record DirectoryNode
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "directory";
    public List<DirectoryNode> Children { get; init; } = [];
    public long? Size { get; init; }
}

/// <summary>
/// Result of the discovery analysis
/// </summary>
public record DiscoveryResult
{
    public List<LanguageInfo> Languages { get; init; } = [];
    public List<FrameworkInfo> Frameworks { get; init; } = [];
    public List<DependencyInfo> Dependencies { get; init; } = [];
    public DirectoryNode DirectoryStructure { get; init; } = new();
}
