using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IRepository<AnalysisRequest> _requestRepository;
    private readonly IRepository<SharedContext> _sharedContextRepository;
    private readonly IRepository<AnalysisJob> _jobRepository;
    private readonly IRepository<Finding> _findingRepository;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IRepository<AnalysisRequest> requestRepository,
        IRepository<SharedContext> sharedContextRepository,
        IRepository<AnalysisJob> jobRepository,
        IRepository<Finding> findingRepository)
    {
        _inventoryRepository = inventoryRepository;
        _requestRepository = requestRepository;
        _sharedContextRepository = sharedContextRepository;
        _jobRepository = jobRepository;
        _findingRepository = findingRepository;
    }

    public async Task<PagedResult<RepositorySummary>> QueryAsync(InventoryFilter filter, CancellationToken cancellationToken)
    {
        var repositories = await _inventoryRepository.GetAllAsync(cancellationToken);
        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        var sharedContexts = await _sharedContextRepository.GetAllAsync(cancellationToken);
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var findings = await _findingRepository.GetAllAsync(cancellationToken);

        var contextsByRequestId = sharedContexts
            .GroupBy(context => context.RequestId)
            .ToDictionary(group => group.Key, group => group.First());

        var findingsByRequestId = BuildFindingsByRequestId(jobs, findings);

        var repositorySummaries = new List<RepositorySummary>();

        foreach (var repository in repositories)
        {
            var repositoryRequests = requests
                .Where(request => request.RepositoryUrl == repository.Url && request.Status == RequestStatus.Completed)
                .Where(request => IsWithinDateRange(request.CompletedAt ?? request.CreatedAt, filter.DateFrom, filter.DateTo))
                .Where(request => MatchesTechnology(filter.Technology, contextsByRequestId, request.Id))
                .Where(request => MatchesDependency(filter.Dependency, contextsByRequestId, request.Id))
                .Where(request => MatchesSeverity(filter.Severity, findingsByRequestId, request.Id))
                .ToList();

            if (repositoryRequests.Count == 0)
            {
                continue;
            }

            var latestRequest = repositoryRequests
                .OrderByDescending(request => request.CompletedAt ?? request.CreatedAt)
                .First();

            var technologies = GetTechnologies(contextsByRequestId, latestRequest.Id);
            var dependencies = GetDependencies(contextsByRequestId, latestRequest.Id);
            var findingsSummary = BuildSeveritySummary(findingsByRequestId.GetValueOrDefault(latestRequest.Id));

            repositorySummaries.Add(new RepositorySummary(
                repository.Id,
                repository.Url,
                repository.Name,
                repository.Provider,
                repository.LastAnalysisAt,
                latestRequest.Id,
                latestRequest.CompletedAt ?? latestRequest.CreatedAt,
                technologies,
                dependencies,
                findingsSummary));
        }

        var ordered = repositorySummaries
            .OrderByDescending(summary => summary.LatestAnalysisAt)
            .ThenBy(summary => summary.Name)
            .ToList();

        var total = ordered.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)filter.Size);
        var paged = ordered
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToList();

        var pagination = new PaginationInfo(filter.Page, filter.Size, total, totalPages);
        return new PagedResult<RepositorySummary>(paged, pagination);
    }

    public async Task<RepositoryTimeline?> GetTimelineAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var repository = await _inventoryRepository.GetByIdAsync(repositoryId, cancellationToken);
        if (repository == null)
        {
            return null;
        }

        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var findings = await _findingRepository.GetAllAsync(cancellationToken);

        var findingsByRequestId = BuildFindingsByRequestId(jobs, findings);

        var timelineEntries = requests
            .Where(request => request.RepositoryUrl == repository.Url && request.Status == RequestStatus.Completed)
            .OrderBy(request => request.CompletedAt ?? request.CreatedAt)
            .Select(request => new RepositoryTimelineEntry(
                request.Id,
                request.CompletedAt ?? request.CreatedAt,
                BuildSeveritySummary(findingsByRequestId.GetValueOrDefault(request.Id))))
            .ToList();

        return new RepositoryTimeline(repository.Id, repository.Url, timelineEntries);
    }

    public async Task<PagedResult<FindingSummary>> QueryFindingsAsync(InventoryFilter filter, CancellationToken cancellationToken)
    {
        var repositories = await _inventoryRepository.GetAllAsync(cancellationToken);
        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        var sharedContexts = await _sharedContextRepository.GetAllAsync(cancellationToken);
        var jobs = await _jobRepository.GetAllAsync(cancellationToken);
        var findings = await _findingRepository.GetAllAsync(cancellationToken);

        var repositoryByUrl = repositories.ToDictionary(repository => repository.Url, repository => repository);
        var requestById = requests.ToDictionary(request => request.Id, request => request);
        var jobById = jobs.ToDictionary(job => job.Id, job => job);
        var contextsByRequestId = sharedContexts
            .GroupBy(context => context.RequestId)
            .ToDictionary(group => group.Key, group => group.First());

        var filteredFindings = new List<FindingSummary>();

        foreach (var finding in findings)
        {
            if (!jobById.TryGetValue(finding.JobId, out var job))
            {
                continue;
            }

            if (!requestById.TryGetValue(job.RequestId, out var request))
            {
                continue;
            }

            if (request.Status != RequestStatus.Completed)
            {
                continue;
            }

            var completedAt = request.CompletedAt ?? request.CreatedAt;
            if (!IsWithinDateRange(completedAt, filter.DateFrom, filter.DateTo))
            {
                continue;
            }

            if (!MatchesTechnology(filter.Technology, contextsByRequestId, request.Id))
            {
                continue;
            }

            if (!MatchesDependency(filter.Dependency, contextsByRequestId, request.Id))
            {
                continue;
            }

            if (filter.Severity.HasValue && SeverityRank(finding.Severity) < SeverityRank(filter.Severity.Value))
            {
                continue;
            }

            if (!repositoryByUrl.TryGetValue(request.RepositoryUrl, out var repository))
            {
                continue;
            }

            filteredFindings.Add(new FindingSummary(
                finding.Id,
                repository.Id,
                repository.Url,
                request.Id,
                finding.Severity.ToString(),
                finding.Category,
                finding.Title,
                finding.Description,
                finding.FilePath,
                completedAt));
        }

        var ordered = filteredFindings
            .OrderByDescending(summary => summary.CompletedAt)
            .ThenByDescending(summary => SeverityRank(Enum.Parse<Severity>(summary.Severity, ignoreCase: true)))
            .ToList();

        var total = ordered.Count;
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)filter.Size);
        var paged = ordered
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .ToList();

        var pagination = new PaginationInfo(filter.Page, filter.Size, total, totalPages);
        return new PagedResult<FindingSummary>(paged, pagination);
    }

    private static Dictionary<Guid, List<Finding>> BuildFindingsByRequestId(
        IReadOnlyList<AnalysisJob> jobs,
        IReadOnlyList<Finding> findings)
    {
        var findingsByJobId = findings
            .GroupBy(finding => finding.JobId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var findingsByRequestId = new Dictionary<Guid, List<Finding>>();

        foreach (var job in jobs)
        {
            if (!findingsByJobId.TryGetValue(job.Id, out var jobFindings))
            {
                continue;
            }

            if (!findingsByRequestId.TryGetValue(job.RequestId, out var requestFindings))
            {
                requestFindings = new List<Finding>();
                findingsByRequestId[job.RequestId] = requestFindings;
            }

            requestFindings.AddRange(jobFindings);
        }

        return findingsByRequestId;
    }

    private static bool IsWithinDateRange(DateTime date, DateTime? from, DateTime? to)
    {
        if (from.HasValue && date < from.Value)
        {
            return false;
        }

        if (to.HasValue && date > to.Value)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesTechnology(
        string? technology,
        IReadOnlyDictionary<Guid, SharedContext> contextsByRequestId,
        Guid requestId)
    {
        if (string.IsNullOrWhiteSpace(technology))
        {
            return true;
        }

        if (!contextsByRequestId.TryGetValue(requestId, out var context))
        {
            return false;
        }

        return context.Languages.Any(language => ContainsIgnoreCase(language, technology))
               || context.Frameworks.Any(framework => ContainsIgnoreCase(framework, technology));
    }

    private static bool MatchesDependency(
        string? dependency,
        IReadOnlyDictionary<Guid, SharedContext> contextsByRequestId,
        Guid requestId)
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            return true;
        }

        if (!contextsByRequestId.TryGetValue(requestId, out var context))
        {
            return false;
        }

        return context.Dependencies.Any(item => ContainsIgnoreCase(item, dependency));
    }

    private static bool MatchesSeverity(
        Severity? minimumSeverity,
        IReadOnlyDictionary<Guid, List<Finding>> findingsByRequestId,
        Guid requestId)
    {
        if (!minimumSeverity.HasValue)
        {
            return true;
        }

        if (!findingsByRequestId.TryGetValue(requestId, out var findings) || findings.Count == 0)
        {
            return false;
        }

        var minimumRank = SeverityRank(minimumSeverity.Value);
        return findings.Any(finding => SeverityRank(finding.Severity) >= minimumRank);
    }

    private static IReadOnlyList<string> GetTechnologies(
        IReadOnlyDictionary<Guid, SharedContext> contextsByRequestId,
        Guid requestId)
    {
        if (!contextsByRequestId.TryGetValue(requestId, out var context))
        {
            return Array.Empty<string>();
        }

        return context.Languages
            .Concat(context.Frameworks)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> GetDependencies(
        IReadOnlyDictionary<Guid, SharedContext> contextsByRequestId,
        Guid requestId)
    {
        if (!contextsByRequestId.TryGetValue(requestId, out var context))
        {
            return Array.Empty<string>();
        }

        return context.Dependencies
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, int> BuildSeveritySummary(IEnumerable<Finding>? findings)
    {
        var summary = Enum.GetValues<Severity>()
            .ToDictionary(severity => severity.ToString(), _ => 0);

        if (findings == null)
        {
            return summary;
        }

        foreach (var group in findings.GroupBy(finding => finding.Severity))
        {
            summary[group.Key.ToString()] = group.Count();
        }

        return summary;
    }

    private static int SeverityRank(Severity severity)
    {
        return severity switch
        {
            Severity.Critical => 5,
            Severity.High => 4,
            Severity.Medium => 3,
            Severity.Low => 2,
            Severity.Informative => 1,
            _ => 0
        };
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
