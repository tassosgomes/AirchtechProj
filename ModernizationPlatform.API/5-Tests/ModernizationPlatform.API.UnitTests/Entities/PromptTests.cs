using FluentAssertions;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class PromptTests
{
    [Fact]
    public void UpdateContent_WhenContentIsEmpty_ShouldThrow()
    {
        var prompt = new Prompt(AnalysisType.Observability, "Initial");

        var action = () => prompt.UpdateContent(string.Empty);

        action.Should().Throw<ArgumentException>();
    }
}
