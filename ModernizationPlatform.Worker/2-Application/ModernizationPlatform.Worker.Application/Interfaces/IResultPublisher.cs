using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IResultPublisher
{
    Task PublishResultAsync(AnalysisResultMessage message, CancellationToken cancellationToken);
}