using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record RepositorySummary(
    Guid Id,
    string Url,
    string Name,
    SourceProvider Provider,
    DateTime? LastAnalysisAt,
    Guid? LatestAnalysisId,
    DateTime? LatestAnalysisAt,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<string> Dependencies,
    IReadOnlyDictionary<string, int> FindingsBySeverity
);
