namespace ModernizationPlatform.Application.DTOs;

public sealed record PaginationInfo(
    int Page,
    int Size,
    int Total,
    int TotalPages
);
