using FluentValidation;
using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Validators;

public sealed class UpdatePromptRequestValidator : AbstractValidator<UpdatePromptRequest>
{
    public UpdatePromptRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required.")
            .MaximumLength(50000)
            .WithMessage("Content must not exceed 50000 characters.");
    }
}
