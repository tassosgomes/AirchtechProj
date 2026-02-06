using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class ConsolidatedResultService : IConsolidatedResultService
{
    private readonly IAnalysisRequestRepository _requestRepository;
    private readonly IFindingRepository _findingRepository;

    public ConsolidatedResultService(
        IAnalysisRequestRepository requestRepository,
        IFindingRepository findingRepository)
    {
        _requestRepository = requestRepository;
        _findingRepository = findingRepository;
    }

    public async Task<ConsolidatedResultDto?> GetConsolidatedResultAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var findings = await _findingRepository.GetByRequestIdAsync(requestId, cancellationToken);
        var findingDtos = findings.Select(f => new FindingDto
        {
            Id = f.Id,
            Severity = f.Severity.ToString(),
            Category = f.Category,
            Title = f.Title,
            Description = f.Description,
            FilePath = f.FilePath
        }).ToList();

        // Calcular sumário
        var totalFindings = findings.Count;
        var bySeverity = findings
            .GroupBy(f => f.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byCategory = findings
            .GroupBy(f => f.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var summary = new ConsolidatedSummaryDto
        {
            TotalFindings = totalFindings,
            BySeverity = bySeverity,
            ByCategory = byCategory
        };

        return new ConsolidatedResultDto
        {
            RequestId = request.Id,
            RepositoryUrl = request.RepositoryUrl,
            CompletedAt = request.CompletedAt ?? DateTime.UtcNow,
            Summary = summary,
            Findings = findingDtos
        };
    }
}
