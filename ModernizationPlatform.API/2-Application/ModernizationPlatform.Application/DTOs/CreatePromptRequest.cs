using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record CreatePromptRequest(
    AnalysisType AnalysisType,
    string Content
);
