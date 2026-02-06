namespace ModernizationPlatform.Worker.Application.DTOs;

public sealed record CopilotRequest(
    string RepositorySnapshot,
    string SharedContextJson,
    string PromptContent,
    string AnalysisType);
