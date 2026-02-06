using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PromptsController : ControllerBase
{
    private readonly IPromptCatalogService _promptCatalogService;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IPromptCatalogService promptCatalogService, ILogger<PromptsController> logger)
    {
        _promptCatalogService = promptCatalogService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all prompts in the catalog
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PromptResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PromptResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var prompts = await _promptCatalogService.GetAllAsync(cancellationToken);
        var response = prompts.Select(p => new PromptResponse(
            p.Id,
            p.AnalysisType,
            p.Content,
            p.CreatedAt,
            p.UpdatedAt
        ));

        return Ok(response);
    }

    /// <summary>
    /// Gets a prompt by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PromptResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var prompt = await _promptCatalogService.GetByIdAsync(id, cancellationToken);

        if (prompt == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Prompt not found",
                detail: $"Prompt with ID '{id}' does not exist."
            );
        }

        var response = new PromptResponse(
            prompt.Id,
            prompt.AnalysisType,
            prompt.Content,
            prompt.CreatedAt,
            prompt.UpdatedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Creates a new prompt or updates existing one for the given analysis type
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PromptResponse>> Create([FromBody] CreatePromptRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var prompt = await _promptCatalogService.CreateOrUpdateAsync(
                request.AnalysisType,
                request.Content,
                cancellationToken
            );

            var response = new PromptResponse(
                prompt.Id,
                prompt.AnalysisType,
                prompt.Content,
                prompt.CreatedAt,
                prompt.UpdatedAt
            );

            return CreatedAtAction(nameof(GetById), new { id = prompt.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: ex.Message
            );
        }
    }

    /// <summary>
    /// Updates an existing prompt
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PromptResponse>> Update(Guid id, [FromBody] UpdatePromptRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updatedPrompt = await _promptCatalogService.UpdateAsync(id, request.Content, cancellationToken);

            if (updatedPrompt == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Prompt not found",
                    detail: $"Prompt with ID '{id}' does not exist."
                );
            }

            var response = new PromptResponse(
                updatedPrompt.Id,
                updatedPrompt.AnalysisType,
                updatedPrompt.Content,
                updatedPrompt.CreatedAt,
                updatedPrompt.UpdatedAt
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: ex.Message
            );
        }
    }
}
