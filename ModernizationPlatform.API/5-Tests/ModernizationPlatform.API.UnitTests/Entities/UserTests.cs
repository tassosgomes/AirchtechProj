using FluentAssertions;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WhenEmailIsEmpty_ShouldThrow()
    {
        var action = () => new User(string.Empty, "hash");

        action.Should().Throw<ArgumentException>();
    }
}