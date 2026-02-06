namespace ModernizationPlatform.Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    PaginationInfo Pagination
);
