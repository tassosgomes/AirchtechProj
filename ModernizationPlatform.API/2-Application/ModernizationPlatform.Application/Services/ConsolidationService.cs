using System.Text.Json;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class ConsolidationService : IConsolidationService
{
    private readonly IAnalysisJobRepository _jobRepository;
    private readonly IFindingRepository _findingRepository;
    private readonly IAnalysisRequestRepository _requestRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConsolidationService(
        IAnalysisJobRepository jobRepository,
        IFindingRepository findingRepository,
        IAnalysisRequestRepository requestRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _findingRepository = findingRepository;
        _requestRepository = requestRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ConsolidateAsync(Guid requestId, CancellationToken cancellationToken)
    {
        // 1. Buscar request e validar
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request {requestId} not found.");
        }

        // 2. Buscar todos os jobs do request
        var jobs = await _jobRepository.GetByRequestIdAsync(requestId, cancellationToken);
        var completedJobs = jobs.Where(j => j.Status == JobStatus.Completed).ToList();

        if (completedJobs.Count == 0)
        {
            throw new InvalidOperationException($"No completed jobs found for request {requestId}.");
        }

        // 3. Normalizar findings de todos os jobs
        var allFindings = new List<Finding>();
        foreach (var job in completedJobs)
        {
            if (string.IsNullOrWhiteSpace(job.OutputJson))
            {
                continue;
            }

            var findings = NormalizeFindings(job.Id, job.OutputJson);
            allFindings.AddRange(findings);
        }

        // 4. Correlacionar findings
        CorrelateFindings(allFindings);

        // 5. Persistir findings
        foreach (var finding in allFindings)
        {
            await _findingRepository.AddAsync(finding, cancellationToken);
        }

        // 6. Atualizar status da request para COMPLETED (assume que está em Consolidating)
        if (request.Status == RequestStatus.Consolidating)
        {
            request.Complete();
        }

        await UpsertRepositoryAsync(request, cancellationToken);

        // 7. Salvar mudanças
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private List<Finding> NormalizeFindings(Guid jobId, string outputJson)
    {
        var findings = new List<Finding>();

        try
        {
            using var document = JsonDocument.Parse(outputJson);
            var root = document.RootElement;

            // Tentar extrair findings de diferentes estruturas possíveis
            JsonElement findingsArray;

            if (root.ValueKind == JsonValueKind.Array)
            {
                // Estrutura: [...] (array direto)
                findings.AddRange(ParseFindingsArray(jobId, root));
            }
            else if (root.TryGetProperty("findings", out findingsArray) && findingsArray.ValueKind == JsonValueKind.Array)
            {
                // Estrutura: { "findings": [...] }
                findings.AddRange(ParseFindingsArray(jobId, findingsArray));
            }
            else if (root.TryGetProperty("issues", out findingsArray) && findingsArray.ValueKind == JsonValueKind.Array)
            {
                // Estrutura alternativa: { "issues": [...] }
                findings.AddRange(ParseFindingsArray(jobId, findingsArray));
            }
            else if (root.TryGetProperty("results", out var results))
            {
                // Estrutura: { "results": { "findings": [...] } }
                if (results.TryGetProperty("findings", out findingsArray) && findingsArray.ValueKind == JsonValueKind.Array)
                {
                    findings.AddRange(ParseFindingsArray(jobId, findingsArray));
                }
            }
        }
        catch (JsonException)
        {
            // Se não conseguir parsear, ignora (job pode ter falhado ou formato inválido)
        }

        return findings;
    }

    private List<Finding> ParseFindingsArray(Guid jobId, JsonElement findingsArray)
    {
        var findings = new List<Finding>();

        foreach (var item in findingsArray.EnumerateArray())
        {
            try
            {
                var severity = ParseSeverity(item);
                var category = GetStringProperty(item, "category", "General") ?? "General";
                var title = GetStringProperty(item, "title", "Untitled Finding") ?? "Untitled Finding";
                var description = GetStringProperty(item, "description", string.Empty) ?? string.Empty;
                
                // Tentar diferentes propriedades para filePath
                var filePath = GetStringProperty(item, "filePath", null) 
                               ?? GetStringProperty(item, "file", null)
                               ?? GetStringProperty(item, "path", null) 
                               ?? "unknown";

                // Normalizar description se estiver vazio
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = title;
                }

                var finding = new Finding(jobId, severity, category, title, description, filePath);
                findings.Add(finding);
            }
            catch
            {
                // Se não conseguir parsear um finding específico, ignora e continua
            }
        }

        return findings;
    }

    private Severity ParseSeverity(JsonElement element)
    {
        var severityStr = GetStringProperty(element, "severity", "Informative");

        if (Enum.TryParse<Severity>(severityStr, ignoreCase: true, out var severity))
        {
            return severity;
        }

        // Mapeamento alternativo de strings comuns
        return severityStr?.ToLowerInvariant() switch
        {
            "critical" or "blocker" => Severity.Critical,
            "high" or "major" or "error" => Severity.High,
            "medium" or "moderate" or "warning" => Severity.Medium,
            "low" or "minor" => Severity.Low,
            _ => Severity.Informative
        };
    }

    private string? GetStringProperty(JsonElement element, string propertyName, string? defaultValue = null)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return defaultValue;
    }

    private void CorrelateFindings(List<Finding> findings)
    {
        // Agrupar findings por filePath
        var findingsByFile = findings
            .GroupBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        // Para cada grupo com múltiplos findings no mesmo arquivo, 
        // podemos adicionar lógica de correlação customizada aqui
        // Por ora, a correlação está implícita pelo agrupamento

        // Agrupar por possível nome de dependência mencionada no título ou description
        var findingsByDependency = findings
            .Where(f => ContainsDependencyKeywords(f.Title) || ContainsDependencyKeywords(f.Description))
            .GroupBy(f => ExtractDependencyName(f.Title) ?? ExtractDependencyName(f.Description))
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
            .ToList();

        // Correlação está disponível através dos agrupamentos
        // Em uma versão futura, poderia adicionar campo CorrelatedWith na entidade Finding
    }

    private bool ContainsDependencyKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var keywords = new[] { "dependência", "dependency", "package", "library", "framework", "nuget", "npm" };
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private string? ExtractDependencyName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Tentar extrair nome entre aspas
        var startQuote = text.IndexOf('"');
        if (startQuote >= 0)
        {
            var endQuote = text.IndexOf('"', startQuote + 1);
            if (endQuote > startQuote)
            {
                return text.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
        }

        // Tentar extrair nome entre parênteses
        var startParen = text.IndexOf('(');
        if (startParen >= 0)
        {
            var endParen = text.IndexOf(')', startParen + 1);
            if (endParen > startParen)
            {
                return text.Substring(startParen + 1, endParen - startParen - 1).Trim();
            }
        }

        return null;
    }

    private async Task UpsertRepositoryAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var existing = await _inventoryRepository.GetByUrlAsync(request.RepositoryUrl, cancellationToken);

        if (existing == null)
        {
            var name = GetRepositoryNameFromUrl(request.RepositoryUrl);
            var repository = new Repository(request.RepositoryUrl, name, request.Provider);
            repository.MarkAnalyzed(request.CompletedAt ?? DateTime.UtcNow);
            await _inventoryRepository.AddAsync(repository, cancellationToken);
            return;
        }

        existing.MarkAnalyzed(request.CompletedAt ?? DateTime.UtcNow);
        _inventoryRepository.Update(existing);
    }

    private static string GetRepositoryNameFromUrl(string repositoryUrl)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            return "unknown";
        }

        if (Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri))
        {
            var segment = uri.Segments.LastOrDefault()?.Trim('/');
            return string.IsNullOrWhiteSpace(segment)
                ? "unknown"
                : segment.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                    ? segment[..^4]
                    : segment;
        }

        return repositoryUrl;
    }
}
