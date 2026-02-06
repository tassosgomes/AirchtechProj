using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IAnalysisJobHandler
{
    Task HandleAsync(AnalysisJobMessage message, CancellationToken cancellationToken);
}