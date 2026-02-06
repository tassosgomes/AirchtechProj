namespace ModernizationPlatform.Domain.Entities;

public class SharedContext
{
    private SharedContext()
    {
    }

    public SharedContext(
        Guid requestId,
        int version,
        IEnumerable<string> languages,
        IEnumerable<string> frameworks,
        IEnumerable<string> dependencies,
        string directoryStructureJson)
    {
        if (requestId == Guid.Empty)
        {
            throw new ArgumentException("Request id is required.", nameof(requestId));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(directoryStructureJson))
        {
            throw new ArgumentException("Directory structure is required.", nameof(directoryStructureJson));
        }

        Id = Guid.NewGuid();
        RequestId = requestId;
        Version = version;
        Languages = languages?.ToList() ?? [];
        Frameworks = frameworks?.ToList() ?? [];
        Dependencies = dependencies?.ToList() ?? [];
        DirectoryStructureJson = directoryStructureJson;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public int Version { get; private set; }
    public List<string> Languages { get; private set; } = [];
    public List<string> Frameworks { get; private set; } = [];
    public List<string> Dependencies { get; private set; } = [];
    public string DirectoryStructureJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
}