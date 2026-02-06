using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.Commands;

public sealed record CreateAnalysisCommand(
    string RepositoryUrl,
    SourceProvider Provider,
    string? AccessToken,
    IReadOnlyList<AnalysisType> SelectedTypes
) : ICommand<AnalysisRequest>;
