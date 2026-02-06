using System.ComponentModel.DataAnnotations;

namespace ModernizationPlatform.Application.Configuration;

public sealed class OrchestrationOptions
{
    public const string SectionName = "Orchestration";

    [Range(1, 1000)]
    public int MaxParallelRequests { get; set; } = 2;

    [Range(1, 3600)]
    public int PollingIntervalSeconds { get; set; } = 10;

    [Range(0, 10)]
    public int MaxJobRetries { get; set; } = 2;

    [Range(1, 86_400)]
    public int JobTimeoutSeconds { get; set; } = 1800;

    [Range(0, int.MaxValue)]
    public int FanOutDependencyThreshold { get; set; } = 0;

    [Range(1, int.MaxValue)]
    public int FanOutDependencyBatchSize { get; set; } = 50;
}
