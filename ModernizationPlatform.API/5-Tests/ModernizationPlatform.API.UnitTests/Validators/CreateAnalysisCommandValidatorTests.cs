using FluentAssertions;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Validators;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Validators;

public class CreateAnalysisCommandValidatorTests
{
    private readonly CreateAnalysisCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithInvalidUrl_ShouldHaveError()
    {
        var command = new CreateAnalysisCommand(
            "not-a-url",
            SourceProvider.GitHub,
            null,
            new List<AnalysisType> { AnalysisType.Security });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateAnalysisCommand.RepositoryUrl));
    }

    [Fact]
    public async Task Validate_WithInvalidProvider_ShouldHaveError()
    {
        var command = new CreateAnalysisCommand(
            "https://example.com/repo",
            (SourceProvider)99,
            null,
            new List<AnalysisType> { AnalysisType.Security });

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateAnalysisCommand.Provider));
    }

    [Fact]
    public async Task Validate_WithNoSelectedTypes_ShouldHaveError()
    {
        var command = new CreateAnalysisCommand(
            "https://example.com/repo",
            SourceProvider.GitHub,
            null,
            Array.Empty<AnalysisType>());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateAnalysisCommand.SelectedTypes));
    }
}
