using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Interfaces;

public interface IAnalysisResultHandler
{
    Task HandleAsync(AnalysisResultMessage message, CancellationToken cancellationToken);
}