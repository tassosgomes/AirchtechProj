namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record AnalysisOutput(
    IReadOnlyList<AnalysisFinding> Findings,
    AnalysisMetadata Metadata,
    long ExecutionDurationMs);
