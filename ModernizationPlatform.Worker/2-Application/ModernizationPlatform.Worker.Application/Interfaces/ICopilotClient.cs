using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface ICopilotClient
{
    Task<CopilotResponse> AnalyzeAsync(CopilotRequest request, CancellationToken cancellationToken);
}
