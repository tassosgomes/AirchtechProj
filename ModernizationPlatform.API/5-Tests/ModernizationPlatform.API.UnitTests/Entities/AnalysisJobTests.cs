using FluentAssertions;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class AnalysisJobTests
{
    [Fact]
    public void Start_WhenStatusIsNotPending_ShouldThrow()
    {
        var job = new AnalysisJob(Guid.NewGuid(), AnalysisType.Security);
        job.Start();

        var action = () => job.Start();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_WhenOutputIsEmpty_ShouldThrow()
    {
        var job = new AnalysisJob(Guid.NewGuid(), AnalysisType.Security);
        job.Start();

        var action = () => job.Complete(string.Empty, TimeSpan.FromSeconds(1));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResetForRetry_WhenJobFailed_ShouldReturnToPending()
    {
        var job = new AnalysisJob(Guid.NewGuid(), AnalysisType.Security);
        job.Start();
        job.Fail("{}", TimeSpan.FromSeconds(1));

        job.ResetForRetry();

        job.Status.Should().Be(JobStatus.Pending);
        job.OutputJson.Should().BeNull();
        job.Duration.Should().BeNull();
    }

    [Fact]
    public void ResetForRetry_WhenJobNotFailed_ShouldThrow()
    {
        var job = new AnalysisJob(Guid.NewGuid(), AnalysisType.Security);

        var action = () => job.ResetForRetry();

        action.Should().Throw<InvalidOperationException>();
    }
}
