using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.Application.DTOs;

public sealed record AnalysisRequestResultsResponse(
    Guid RequestId,
    RequestStatus Status,
    IReadOnlyList<AnalysisJobResultResponse> Jobs
);
