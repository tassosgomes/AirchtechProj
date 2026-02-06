using FluentValidation;
using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Validators;

public sealed class CreatePromptRequestValidator : AbstractValidator<CreatePromptRequest>
{
    public CreatePromptRequestValidator()
    {
        RuleFor(x => x.AnalysisType)
            .IsInEnum()
            .WithMessage("AnalysisType must be a valid value (Obsolescence, Security, Observability, Documentation).");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.")
            .MaximumLength(50000)
            .WithMessage("Content must not exceed 50000 characters.");
    }
}
