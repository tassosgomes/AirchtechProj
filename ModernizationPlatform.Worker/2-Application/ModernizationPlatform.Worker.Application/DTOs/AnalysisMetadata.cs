namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record AnalysisMetadata(
    string AnalysisType,
    int TotalFindings,
    long ExecutionDurationMs);
