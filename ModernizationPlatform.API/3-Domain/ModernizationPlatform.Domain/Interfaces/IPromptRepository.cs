using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Interfaces;

public interface IPromptRepository : IRepository<Prompt>
{
    Task<Prompt?> GetByAnalysisTypeAsync(AnalysisType analysisType, CancellationToken cancellationToken);
}