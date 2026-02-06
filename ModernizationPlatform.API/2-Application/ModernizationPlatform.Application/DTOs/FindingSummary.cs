namespace ModernizationPlatform.Application.DTOs;

public sealed record FindingSummary(
    Guid Id,
    Guid RepositoryId,
    string RepositoryUrl,
    Guid AnalysisId,
    string Severity,
    string Category,
    string Title,
    string Description,
    string FilePath,
    DateTime CompletedAt
);
