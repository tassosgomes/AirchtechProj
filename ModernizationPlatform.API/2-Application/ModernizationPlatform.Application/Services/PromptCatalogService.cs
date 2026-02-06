using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class PromptCatalogService : IPromptCatalogService
{
    private readonly IPromptRepository _promptRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromptCatalogService(IPromptRepository promptRepository, IUnitOfWork unitOfWork)
    {
        _promptRepository = promptRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Prompt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _promptRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Prompt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _promptRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Prompt?> GetByAnalysisTypeAsync(AnalysisType analysisType, CancellationToken cancellationToken = default)
    {
        return await _promptRepository.GetByAnalysisTypeAsync(analysisType, cancellationToken);
    }

    public async Task<Prompt> CreateOrUpdateAsync(AnalysisType analysisType, string content, CancellationToken cancellationToken = default)
    {
        var existingPrompt = await GetByAnalysisTypeAsync(analysisType, cancellationToken);

        if (existingPrompt != null)
        {
            existingPrompt.UpdateContent(content);
            _promptRepository.Update(existingPrompt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return existingPrompt;
        }

        var newPrompt = new Prompt(analysisType, content);
        await _promptRepository.AddAsync(newPrompt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return newPrompt;
    }

    public async Task<Prompt?> UpdateAsync(Guid id, string content, CancellationToken cancellationToken = default)
    {
        var prompt = await _promptRepository.GetByIdAsync(id, cancellationToken);
        if (prompt == null)
        {
            return null;
        }

        prompt.UpdateContent(content);
        _promptRepository.Update(prompt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return prompt;
    }
}
