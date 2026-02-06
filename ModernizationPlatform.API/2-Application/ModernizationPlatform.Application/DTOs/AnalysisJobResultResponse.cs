using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record AnalysisJobResultResponse(
    AnalysisType AnalysisType,
    JobStatus Status,
    string? OutputJson,
    long? DurationMs
);
