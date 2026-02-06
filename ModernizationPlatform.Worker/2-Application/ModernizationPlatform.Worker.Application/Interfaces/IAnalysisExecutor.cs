using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IAnalysisExecutor
{
    Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken cancellationToken);
}
