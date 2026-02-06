namespace ModernizationPlatform.Application.DTOs;

public sealed record AnalysisResultMessage(
    Guid JobId,
    Guid RequestId,
    string AnalysisType,
    string Status,
    string OutputJson,
    long DurationMs,
    string? ErrorMessage);