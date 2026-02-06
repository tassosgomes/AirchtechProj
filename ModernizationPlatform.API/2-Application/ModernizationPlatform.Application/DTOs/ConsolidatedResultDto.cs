namespace ModernizationPlatform.Application.DTOs;

public sealed record ConsolidatedResultDto
{
    public required Guid RequestId { get; init; }
    public required string RepositoryUrl { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required ConsolidatedSummaryDto Summary { get; init; }
    public required List<FindingDto> Findings { get; init; }
}

public sealed record ConsolidatedSummaryDto
{
    public required int TotalFindings { get; init; }
    public required Dictionary<string, int> BySeverity { get; init; }
    public required Dictionary<string, int> ByCategory { get; init; }
}

public sealed record FindingDto
{
    public required Guid Id { get; init; }
    public required string Severity { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
}
