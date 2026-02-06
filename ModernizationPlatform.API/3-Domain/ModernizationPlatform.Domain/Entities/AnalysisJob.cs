using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Domain.Entities;

public class AnalysisJob
{
    private AnalysisJob()
    {
    }

    public AnalysisJob(Guid requestId, AnalysisType type)
    {
        if (requestId == Guid.Empty)
        {
            throw new ArgumentException("Request id is required.", nameof(requestId));
        }

        Id = Guid.NewGuid();
        RequestId = requestId;
        Type = type;
        Status = JobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public AnalysisType Type { get; private set; }
    public JobStatus Status { get; private set; }
    public string? OutputJson { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void Start()
    {
        EnsureStatus(JobStatus.Pending, nameof(Start));
        Status = JobStatus.Running;
    }

    public void Complete(string outputJson, TimeSpan duration)
    {
        EnsureStatus(JobStatus.Running, nameof(Complete));

        if (string.IsNullOrWhiteSpace(outputJson))
        {
            throw new ArgumentException("Output is required.", nameof(outputJson));
        }

        OutputJson = outputJson;
        Duration = duration;
        Status = JobStatus.Completed;
    }

    public void Fail(string? outputJson, TimeSpan? duration)
    {
        if (Status is JobStatus.Completed or JobStatus.Failed)
        {
            throw new InvalidOperationException("Job is already finalized.");
        }

        OutputJson = outputJson;
        Duration = duration;
        Status = JobStatus.Failed;
    }

    private void EnsureStatus(JobStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot {action} when status is {Status}.");
        }
    }
}