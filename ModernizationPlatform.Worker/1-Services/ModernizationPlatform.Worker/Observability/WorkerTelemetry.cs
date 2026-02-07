using System.Diagnostics;

namespace ModernizationPlatform.Worker.Observability;

public static class WorkerTelemetry
{
    public const string ActivitySourceName = "ModernizationPlatform.Worker";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
