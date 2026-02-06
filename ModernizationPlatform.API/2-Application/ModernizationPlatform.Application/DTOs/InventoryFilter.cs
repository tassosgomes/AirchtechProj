using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record InventoryFilter(
    string? Technology,
    string? Dependency,
    Severity? Severity,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page,
    int Size
);
