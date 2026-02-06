namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record AnalysisJobMessage(
    Guid JobId,
    Guid RequestId,
    string RepositoryUrl,
    string Provider,
    string AccessToken,
    string SharedContextJson,
    string PromptContent,
    string AnalysisType,
    int TimeoutSeconds);