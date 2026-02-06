using System.ComponentModel.DataAnnotations;

namespace ModernizationPlatform.Worker.Application.Configuration;

public sealed class AnalysisTimeoutOptions
{
    public const string SectionName = "AnalysisTimeouts";

    [Range(1, 86_400)]
    public int DefaultSeconds { get; init; } = 1800;

    public Dictionary<string, int> PerAnalysisTypeSeconds { get; init; } = new();

    public int ResolveTimeoutSeconds(string analysisType, int requestedSeconds)
    {
        if (requestedSeconds > 0)
        {
            return requestedSeconds;
        }

        if (!string.IsNullOrWhiteSpace(analysisType))
        {
            foreach (var entry in PerAnalysisTypeSeconds)
            {
                if (string.Equals(entry.Key, analysisType, StringComparison.OrdinalIgnoreCase) && entry.Value > 0)
                {
                    return entry.Value;
                }
            }
        }

        return DefaultSeconds > 0 ? DefaultSeconds : 1800;
    }
}
