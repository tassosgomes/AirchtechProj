using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.Interfaces;

public interface IPromptCatalogService
{
    Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Prompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Prompt?> GetByAnalysisTypeAsync(AnalysisType analysisType, CancellationToken cancellationToken = default);
    Task<Prompt> CreateOrUpdateAsync(AnalysisType analysisType, string content, CancellationToken cancellationToken = default);
    Task<Prompt?> UpdateAsync(Guid id, string content, CancellationToken cancellationToken = default);
}
