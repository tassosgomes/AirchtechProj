using FluentAssertions;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;

namespace ModernizationPlatform.API.UnitTests.Entities;

public class AnalysisRequestTests
{
    [Fact]
    public void StartDiscovery_WhenStatusIsNotQueued_ShouldThrow()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();

        var action = () => request.StartDiscovery();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartAnalysis_WhenStatusIsNotDiscoveryRunning_ShouldThrow()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);

        var action = () => request.StartAnalysis();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_WhenStatusIsNotConsolidating_ShouldThrow()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();

        var action = () => request.Complete();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartConsolidation_WhenStatusIsNotAnalysisRunning_ShouldThrow()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);

        var action = () => request.StartConsolidation();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fail_WhenStatusIsCompleted_ShouldThrow()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);
        request.StartDiscovery();
        request.StartAnalysis();
        request.StartConsolidation();
        request.Complete();

        var action = () => request.Fail();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IncrementRetry_ShouldIncreaseRetryCount()
    {
        var request = new AnalysisRequest("https://example.com/repo", SourceProvider.GitHub, [AnalysisType.Security]);

        request.IncrementRetry();

        request.RetryCount.Should().Be(1);
    }
}