using System.Text.Json;
using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.API.UnitTests.Messaging;

public class AnalysisMessageSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AnalysisJobMessage_Should_RoundTrip_Serialization()
    {
        var message = new AnalysisJobMessage(
            JobId: Guid.NewGuid(),
            RequestId: Guid.NewGuid(),
            RepositoryUrl: "https://github.com/org/repo",
            Provider: "GitHub",
            AccessToken: "token",
            SharedContextJson: "{}",
            PromptContent: "Analyze",
            AnalysisType: "Security",
            TimeoutSeconds: 300);

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<AnalysisJobMessage>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(message.JobId, roundTrip.JobId);
        Assert.Equal(message.RequestId, roundTrip.RequestId);
        Assert.Equal(message.RepositoryUrl, roundTrip.RepositoryUrl);
    }

    [Fact]
    public void AnalysisResultMessage_Should_RoundTrip_Serialization()
    {
        var message = new AnalysisResultMessage(
            JobId: Guid.NewGuid(),
            RequestId: Guid.NewGuid(),
            AnalysisType: "Security",
            Status: "COMPLETED",
            OutputJson: "{\"ok\":true}",
            DurationMs: 1234,
            ErrorMessage: null);

        var json = JsonSerializer.Serialize(message, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<AnalysisResultMessage>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(message.JobId, roundTrip.JobId);
        Assert.Equal(message.RequestId, roundTrip.RequestId);
        Assert.Equal(message.Status, roundTrip.Status);
    }
}