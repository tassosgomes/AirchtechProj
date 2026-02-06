using FluentAssertions;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class FindingTests
{
    [Fact]
    public void Constructor_WhenCategoryIsEmpty_ShouldThrow()
    {
        var action = () => new Finding(Guid.NewGuid(), Severity.High, string.Empty, "Title", "Description", "path/file.cs");

        action.Should().Throw<ArgumentException>();
    }
}
