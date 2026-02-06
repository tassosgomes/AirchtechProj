using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("repositories")]
    [ProducesResponseType(typeof(PagedResult<RepositorySummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<RepositorySummary>>> GetRepositories(
        [FromQuery] string? technology,
        [FromQuery] string? dependency,
        [FromQuery] Severity? severity,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int _page = 1,
        [FromQuery] int _size = 20,
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

        var filter = new InventoryFilter(technology, dependency, severity, dateFrom, dateTo, _page, _size);
        var result = await _inventoryService.QueryAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("repositories/{id:guid}/timeline")]
    [ProducesResponseType(typeof(RepositoryTimeline), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RepositoryTimeline>> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var timeline = await _inventoryService.GetTimelineAsync(id, cancellationToken);
        if (timeline == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Repository not found",
                detail: $"Repository with ID '{id}' does not exist.");
        }

        return Ok(timeline);
    }

    [HttpGet("findings")]
    [ProducesResponseType(typeof(PagedResult<FindingSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<FindingSummary>>> GetFindings(
        [FromQuery] string? technology,
        [FromQuery] string? dependency,
        [FromQuery] Severity? severity,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int _page = 1,
        [FromQuery] int _size = 20,
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

        var filter = new InventoryFilter(technology, dependency, severity, dateFrom, dateTo, _page, _size);
        var result = await _inventoryService.QueryFindingsAsync(filter, cancellationToken);
        return Ok(result);
    }
}
