using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record AnalysisRequestResponse(
    Guid Id,
    string RepositoryUrl,
    SourceProvider Provider,
    RequestStatus Status,
    int? QueuePosition,
    IReadOnlyList<AnalysisType> SelectedTypes,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
