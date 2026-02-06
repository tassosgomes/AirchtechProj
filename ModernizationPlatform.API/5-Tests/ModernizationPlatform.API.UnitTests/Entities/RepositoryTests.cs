using FluentAssertions;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class RepositoryTests
{
    [Fact]
    public void Constructor_WhenUrlIsEmpty_ShouldThrow()
    {
        var action = () => new Repository(string.Empty, "Repo", SourceProvider.GitHub);

        action.Should().Throw<ArgumentException>();
    }
}
