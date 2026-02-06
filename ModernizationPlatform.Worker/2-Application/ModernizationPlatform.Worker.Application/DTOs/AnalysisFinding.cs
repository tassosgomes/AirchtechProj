namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record AnalysisFinding(
    string Severity,
    string Category,
    string Title,
    string Description,
    string FilePath);
