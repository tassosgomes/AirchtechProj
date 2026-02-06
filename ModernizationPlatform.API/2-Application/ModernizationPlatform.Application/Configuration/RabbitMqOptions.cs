using System.ComponentModel.DataAnnotations;

namespace ModernizationPlatform.Application.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    [Required]
    public string Username { get; init; } = "guest";

    [Required]
    public string Password { get; init; } = "guest";

    [Range(1, 1000)]
    public ushort PrefetchCount { get; init; } = 10;
}