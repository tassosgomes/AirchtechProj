using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Interfaces;

public interface IJobPublisher
{
    Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken);
}