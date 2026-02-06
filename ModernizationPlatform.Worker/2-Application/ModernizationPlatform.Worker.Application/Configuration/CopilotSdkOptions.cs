using System.ComponentModel.DataAnnotations;

namespace ModernizationPlatform.Worker.Application.Configuration;

public sealed class CopilotSdkOptions
{
    public const string SectionName = "CopilotSdk";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    public string? ApiKey { get; init; }
}
