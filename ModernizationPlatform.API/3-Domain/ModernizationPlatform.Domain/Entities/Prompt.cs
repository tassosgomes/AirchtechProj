using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Entities;

public class Prompt
{
    private Prompt()
    {
    }

    public Prompt(AnalysisType analysisType, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        Id = Guid.NewGuid();
        AnalysisType = analysisType;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public AnalysisType AnalysisType { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void UpdateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}