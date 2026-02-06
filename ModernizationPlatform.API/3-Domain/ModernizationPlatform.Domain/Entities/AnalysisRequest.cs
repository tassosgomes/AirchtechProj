using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Entities;

public class AnalysisRequest
{
    private AnalysisRequest()
    {
    }

    public AnalysisRequest(string repositoryUrl, SourceProvider provider, IEnumerable<AnalysisType> selectedTypes)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            throw new ArgumentException("Repository URL is required.", nameof(repositoryUrl));
        }

        if (selectedTypes is null)
        {
            throw new ArgumentNullException(nameof(selectedTypes));
        }

        var types = selectedTypes.ToList();
        if (types.Count == 0)
        {
            throw new ArgumentException("At least one analysis type is required.", nameof(selectedTypes));
        }

        Id = Guid.NewGuid();
        RepositoryUrl = repositoryUrl;
        Provider = provider;
        Status = RequestStatus.Queued;
        SelectedTypes = types;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string RepositoryUrl { get; private set; } = string.Empty;
    public SourceProvider Provider { get; private set; }
    public RequestStatus Status { get; private set; }
    public List<AnalysisType> SelectedTypes { get; private set; } = [];
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public void StartDiscovery()
    {
        EnsureStatus(RequestStatus.Queued, nameof(StartDiscovery));
        Status = RequestStatus.DiscoveryRunning;
    }

    public void StartAnalysis()
    {
        EnsureStatus(RequestStatus.DiscoveryRunning, nameof(StartAnalysis));
        Status = RequestStatus.AnalysisRunning;
    }

    public void StartConsolidation()
    {
        EnsureStatus(RequestStatus.AnalysisRunning, nameof(StartConsolidation));
        Status = RequestStatus.Consolidating;
    }

    public void Complete()
    {
        EnsureStatus(RequestStatus.Consolidating, nameof(Complete));
        Status = RequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status is RequestStatus.Completed or RequestStatus.Failed)
        {
            throw new InvalidOperationException("Request is already finalized.");
        }

        Status = RequestStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
    }

    private void EnsureStatus(RequestStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot {action} when status is {Status}.");
        }
    }
}