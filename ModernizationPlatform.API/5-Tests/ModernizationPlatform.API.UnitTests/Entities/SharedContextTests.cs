using FluentAssertions;
using ModernizationPlatform.Domain.Entities;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class SharedContextTests
{
    [Fact]
    public void Constructor_WhenVersionIsInvalid_ShouldThrow()
    {
        var action = () => new SharedContext(
            Guid.NewGuid(),
            0,
            ["C#"],
            ["ASP.NET"],
            ["Npgsql"],
            "{}");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
