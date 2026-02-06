namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record AnalysisInput(
    string RepositoryUrl,
    string Provider,
    string? AccessToken,
    string SharedContextJson,
    string PromptContent,
    string AnalysisType,
    int TimeoutSeconds);
