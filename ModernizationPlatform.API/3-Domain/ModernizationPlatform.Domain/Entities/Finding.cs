using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Entities;

public class Finding
{
    private Finding()
    {
    }

    public Finding(Guid jobId, Severity severity, string category, string title, string description, string filePath)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        Id = Guid.NewGuid();
        JobId = jobId;
        Severity = severity;
        Category = category;
        Title = title;
        Description = description;
        FilePath = filePath;
    }

    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Severity Severity { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
}