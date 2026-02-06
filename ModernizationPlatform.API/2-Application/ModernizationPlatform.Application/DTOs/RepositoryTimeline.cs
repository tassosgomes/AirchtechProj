namespace ModernizationPlatform.Application.DTOs;

public sealed record RepositoryTimeline(
    Guid RepositoryId,
    string RepositoryUrl,
    IReadOnlyList<RepositoryTimelineEntry> Analyses
);

public sealed record RepositoryTimelineEntry(
    Guid AnalysisId,
    DateTime CompletedAt,
    IReadOnlyDictionary<string, int> Summary
);
