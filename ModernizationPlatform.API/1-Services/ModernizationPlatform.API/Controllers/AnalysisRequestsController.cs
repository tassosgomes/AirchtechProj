using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.API.Controllers;

[ApiController]
[Route("api/v1/analysis-requests")]
[Authorize]
public sealed class AnalysisRequestsController : ControllerBase
{
    private readonly IOrchestrationService _orchestrationService;
    private readonly IAnalysisRequestRepository _analysisRequestRepository;
    private readonly IAnalysisJobRepository _analysisJobRepository;

    public AnalysisRequestsController(
        IOrchestrationService orchestrationService,
        IAnalysisRequestRepository analysisRequestRepository,
        IAnalysisJobRepository analysisJobRepository)
    {
        _orchestrationService = orchestrationService;
        _analysisRequestRepository = analysisRequestRepository;
        _analysisJobRepository = analysisJobRepository;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnalysisRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnalysisRequestResponse>> Create(
        [FromBody] CreateAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateAnalysisCommand(
                request.RepositoryUrl,
                request.Provider,
                request.AccessToken,
                request.SelectedTypes);

            var analysisRequest = await _orchestrationService.CreateRequestAsync(command, cancellationToken);
            var queuePosition = await GetQueuePositionAsync(analysisRequest, cancellationToken);

            var response = MapResponse(analysisRequest, queuePosition);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ToValidationProblemDetails(ex.Errors));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(AnalysisRequestListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnalysisRequestListResponse>> GetAll(
        [FromQuery] int _page = 1,
        [FromQuery] int _size = 10,
        CancellationToken cancellationToken = default)
    {
        if (_page < 1 || _size < 1)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "_page and _size must be greater than zero."
            });
        }

        var total = await _analysisRequestRepository.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)_size);

        var requests = await _analysisRequestRepository.GetPagedAsync(_page, _size, cancellationToken);
        var data = requests
            .Select(request => MapResponse(request, queuePosition: null))
            .ToList();

        var pagination = new PaginationInfo(_page, _size, total, totalPages);
        return Ok(new AnalysisRequestListResponse(data, pagination));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnalysisRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnalysisRequestResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _analysisRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Analysis request not found",
                detail: $"Analysis request with ID '{id}' does not exist.");
        }

        var queuePosition = await GetQueuePositionAsync(request, cancellationToken);
        return Ok(MapResponse(request, queuePosition));
    }

    [HttpGet("{id:guid}/results")]
    [ProducesResponseType(typeof(AnalysisRequestResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnalysisRequestResultsResponse>> GetResults(Guid id, CancellationToken cancellationToken)
    {
        var request = await _analysisRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Analysis request not found",
                detail: $"Analysis request with ID '{id}' does not exist.");
        }

        var jobs = await _analysisJobRepository.GetByRequestIdAsync(id, cancellationToken);
        var jobResponses = jobs
            .Select(job => new AnalysisJobResultResponse(
                job.Type,
                job.Status,
                job.OutputJson,
                job.Duration.HasValue ? (long?)job.Duration.Value.TotalMilliseconds : null))
            .ToList();

        var response = new AnalysisRequestResultsResponse(id, request.Status, jobResponses);
        return Ok(response);
    }

    private async Task<int?> GetQueuePositionAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (request.Status != RequestStatus.Queued)
        {
            return null;
        }

        var countBefore = await _analysisRequestRepository.CountQueuedBeforeAsync(request.CreatedAt, cancellationToken);
        return countBefore + 1;
    }

    private static AnalysisRequestResponse MapResponse(AnalysisRequest request, int? queuePosition)
    {
        return new AnalysisRequestResponse(
            request.Id,
            request.RepositoryUrl,
            request.Provider,
            request.Status,
            queuePosition,
            request.SelectedTypes,
            request.CreatedAt,
            request.CompletedAt);
    }

    private static ValidationProblemDetails ToValidationProblemDetails(IEnumerable<ValidationFailure> errors)
    {
        var modelState = errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };
    }
}
