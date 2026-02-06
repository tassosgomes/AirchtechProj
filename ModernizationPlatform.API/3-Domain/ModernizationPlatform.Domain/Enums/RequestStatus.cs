namespace ModernizationPlatform.Domain.Enums;

public enum RequestStatus
{
    Queued,
    DiscoveryRunning,
    AnalysisRunning,
    Consolidating,
    Completed,
    Failed
}