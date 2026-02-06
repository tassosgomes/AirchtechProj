using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record CreateAnalysisRequest(
    string RepositoryUrl,
    SourceProvider Provider,
    string? AccessToken,
    IReadOnlyList<AnalysisType> SelectedTypes
);
