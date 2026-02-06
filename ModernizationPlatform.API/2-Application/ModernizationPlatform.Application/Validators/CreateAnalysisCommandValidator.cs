using FluentValidation;
using ModernizationPlatform.Application.Commands;

namespace ModernizationPlatform.Application.Validators;

public sealed class CreateAnalysisCommandValidator : AbstractValidator<CreateAnalysisCommand>
{
    public CreateAnalysisCommandValidator()
    {
        RuleFor(x => x.RepositoryUrl)
            .NotEmpty()
            .WithMessage("RepositoryUrl is required.")
            .Must(BeValidRepositoryUrl)
            .WithMessage("RepositoryUrl must be a valid HTTP or HTTPS URL.");

        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("Provider must be a valid value (GitHub, AzureDevOps).");

        RuleFor(x => x.SelectedTypes)
            .NotNull()
            .WithMessage("SelectedTypes is required.")
            .Must(types => types != null && types.Count > 0)
            .WithMessage("At least one analysis type must be selected.");

        RuleForEach(x => x.SelectedTypes)
            .IsInEnum()
            .WithMessage("SelectedTypes contains an invalid value.");
    }

    private static bool BeValidRepositoryUrl(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https";
    }
}
