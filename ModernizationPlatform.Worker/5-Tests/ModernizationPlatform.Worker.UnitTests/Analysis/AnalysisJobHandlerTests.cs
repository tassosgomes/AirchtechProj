using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Consumers;
using Xunit;

namespace ModernizationPlatform.Worker.UnitTests.Analysis;

public class AnalysisJobHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSuccess_PublishesRunningAndCompleted()
    {
        var publisher = new FakeResultPublisher();
        var executor = new SuccessExecutor();
        var options = Options.Create(new AnalysisTimeoutOptions { DefaultSeconds = 60 });
        var handler = new AnalysisJobHandler(executor, publisher, options, NullLogger<AnalysisJobHandler>.Instance);

        await handler.HandleAsync(CreateMessage(), CancellationToken.None);

        Assert.Equal(2, publisher.Messages.Count);
        Assert.Equal("RUNNING", publisher.Messages[0].Status);
        Assert.Equal("COMPLETED", publisher.Messages[1].Status);
        Assert.False(string.IsNullOrWhiteSpace(publisher.Messages[1].OutputJson));
    }

    [Fact]
    public async Task HandleAsync_WhenParsingFails_PublishesFailedWithRawOutput()
    {
        var publisher = new FakeResultPublisher();
        var executor = new ParsingFailureExecutor();
        var options = Options.Create(new AnalysisTimeoutOptions { DefaultSeconds = 60 });
        var handler = new AnalysisJobHandler(executor, publisher, options, NullLogger<AnalysisJobHandler>.Instance);

        await handler.HandleAsync(CreateMessage(), CancellationToken.None);

        Assert.Equal("FAILED", publisher.Messages[1].Status);
        Assert.Contains("RAW_OUTPUT", publisher.Messages[1].ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task HandleAsync_WhenTimeoutOccurs_PublishesFailed()
    {
        var publisher = new FakeResultPublisher();
        var executor = new TimeoutExecutor();
        var options = Options.Create(new AnalysisTimeoutOptions { DefaultSeconds = 1 });
        var handler = new AnalysisJobHandler(executor, publisher, options, NullLogger<AnalysisJobHandler>.Instance);

        await handler.HandleAsync(CreateMessage(timeoutSeconds: 1), CancellationToken.None);

        Assert.Equal("FAILED", publisher.Messages[1].Status);
    }

    private static AnalysisJobMessage CreateMessage(int timeoutSeconds = 60)
    {
        return new AnalysisJobMessage(
            JobId: Guid.NewGuid(),
            RequestId: Guid.NewGuid(),
            RepositoryUrl: "https://github.com/org/repo",
            Provider: "GitHub",
            AccessToken: "token",
            SharedContextJson: "{}",
            PromptContent: "Analyze",
            AnalysisType: "Security",
            TimeoutSeconds: timeoutSeconds);
    }

    private sealed class FakeResultPublisher : IResultPublisher
    {
        public List<AnalysisResultMessage> Messages { get; } = new();

        public Task PublishResultAsync(AnalysisResultMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessExecutor : IAnalysisExecutor
    {
        public Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken cancellationToken)
        {
            var output = new AnalysisOutput(
                [new AnalysisFinding("High", "Security", "Issue", "Desc", "file.cs")],
                new AnalysisMetadata(input.AnalysisType, 1, 120),
                120);

            return Task.FromResult(output);
        }
    }

    private sealed class ParsingFailureExecutor : IAnalysisExecutor
    {
        public Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken cancellationToken)
        {
            throw new AnalysisOutputParsingException("Invalid", "RAW_OUTPUT");
        }
    }

    private sealed class TimeoutExecutor : IAnalysisExecutor
    {
        public async Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new AnalysisOutput([], new AnalysisMetadata(input.AnalysisType, 0, 0), 0);
        }
    }
}
