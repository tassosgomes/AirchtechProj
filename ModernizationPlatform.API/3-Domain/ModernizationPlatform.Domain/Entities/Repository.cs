using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Entities;

public class Repository
{
    private Repository()
    {
    }

    public Repository(string url, string name, SourceProvider provider)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Url is required.", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Url = url;
        Name = name;
        Provider = provider;
    }

    public Guid Id { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public SourceProvider Provider { get; private set; }
    public DateTime? LastAnalysisAt { get; private set; }

    public void MarkAnalyzed(DateTime analyzedAt)
    {
        LastAnalysisAt = analyzedAt;
    }
}