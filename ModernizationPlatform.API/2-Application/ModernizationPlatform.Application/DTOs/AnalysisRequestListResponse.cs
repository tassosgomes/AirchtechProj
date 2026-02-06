namespace ModernizationPlatform.Application.DTOs;

public sealed record AnalysisRequestListResponse(
    IReadOnlyList<AnalysisRequestResponse> Data,
    PaginationInfo Pagination
);
