using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record PromptResponse(
    Guid Id,
    AnalysisType AnalysisType,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
