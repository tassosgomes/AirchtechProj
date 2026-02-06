using System.Collections.Concurrent;
using ModernizationPlatform.Application.DTOs;

namespace ModernizationPlatform.Application.Services;

public sealed class OrchestrationStateStore
{
    private readonly ConcurrentDictionary<Guid, string> _accessTokens = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<AnalysisResultMessage>> _jobResults = new();
    private readonly ConcurrentDictionary<Guid, int> _jobRetryCounts = new();

    public void StoreAccessToken(Guid requestId, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        _accessTokens[requestId] = accessToken;
    }

    public string? GetAccessToken(Guid requestId)
    {
        return _accessTokens.TryGetValue(requestId, out var token) ? token : null;
    }

    public void ClearRequest(Guid requestId)
    {
        _accessTokens.TryRemove(requestId, out _);
    }

    public Task<AnalysisResultMessage> WaitForJobResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var tcs = _jobResults.GetOrAdd(jobId, _ =>
            new TaskCompletionSource<AnalysisResultMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return tcs.Task;
    }

    public void CompleteJobResult(AnalysisResultMessage message)
    {
        var tcs = _jobResults.GetOrAdd(message.JobId, _ =>
            new TaskCompletionSource<AnalysisResultMessage>(TaskCreationOptions.RunContinuationsAsynchronously));
        tcs.TrySetResult(message);
    }

    public void ClearJobResult(Guid jobId)
    {
        _jobResults.TryRemove(jobId, out _);
    }

    public void ClearJobState(Guid jobId)
    {
        _jobResults.TryRemove(jobId, out _);
        _jobRetryCounts.TryRemove(jobId, out _);
    }

    public int IncrementRetryCount(Guid jobId)
    {
        return _jobRetryCounts.AddOrUpdate(jobId, 1, (_, current) => current + 1);
    }

    public int GetRetryCount(Guid jobId)
    {
        return _jobRetryCounts.TryGetValue(jobId, out var count) ? count : 0;
    }
}
