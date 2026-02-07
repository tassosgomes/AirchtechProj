using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;
using ModernizationPlatform.Worker.Application.Services;
using ModernizationPlatform.Worker.Observability;
using Sentry;
using Serilog.Context;

namespace ModernizationPlatform.Worker.Consumers;

public sealed class AnalysisJobHandler : IAnalysisJobHandler
{
    private const int MaxErrorMessageLength = 4000;
    private readonly IResultPublisher _resultPublisher;
    private readonly IAnalysisExecutor _analysisExecutor;
    private readonly AnalysisTimeoutOptions _timeoutOptions;
    private readonly ILogger<AnalysisJobHandler> _logger;

    public AnalysisJobHandler(
        IAnalysisExecutor analysisExecutor,
        IResultPublisher resultPublisher,
        IOptions<AnalysisTimeoutOptions> timeoutOptions,
        ILogger<AnalysisJobHandler> logger)
    {
        _analysisExecutor = analysisExecutor;
        _resultPublisher = resultPublisher;
        _timeoutOptions = timeoutOptions.Value;
        _logger = logger;
    }

    public async Task HandleAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
    {
        using var activity = WorkerTelemetry.ActivitySource.StartActivity("analysis.job", ActivityKind.Consumer);
        activity?.SetTag("requestId", message.RequestId.ToString());
        activity?.SetTag("jobId", message.JobId.ToString());
        activity?.SetTag("analysisType", message.AnalysisType);

        using var requestIdScope = LogContext.PushProperty("requestId", message.RequestId);
        using var jobIdScope = LogContext.PushProperty("jobId", message.JobId);
        using var analysisTypeScope = LogContext.PushProperty("analysisType", message.AnalysisType);

        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("requestId", message.RequestId.ToString());
            scope.SetTag("jobId", message.JobId.ToString());
            scope.SetTag("analysisType", message.AnalysisType);
        });

        _logger.LogInformation(
            "Job recebido. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType}",
            message.JobId,
            message.RequestId,
            message.AnalysisType);

        var timeoutSeconds = _timeoutOptions.ResolveTimeoutSeconds(message.AnalysisType, message.TimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();

        await _resultPublisher.PublishResultAsync(new AnalysisResultMessage(
            message.JobId,
            message.RequestId,
            message.AnalysisType,
            "RUNNING",
            OutputJson: "{}",
            DurationMs: 0,
            ErrorMessage: null),
            cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var input = new AnalysisInput(
                message.RepositoryUrl,
                message.Provider,
                message.AccessToken,
                message.SharedContextJson,
                message.PromptContent,
                message.AnalysisType,
                timeoutSeconds);

            var output = await _analysisExecutor.ExecuteAsync(input, timeoutCts.Token);
            var outputJson = AnalysisJsonSerializer.Serialize(output);

            await _resultPublisher.PublishResultAsync(new AnalysisResultMessage(
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                "COMPLETED",
                OutputJson: outputJson,
                DurationMs: output.ExecutionDurationMs,
                ErrorMessage: null),
                cancellationToken);

            _logger.LogInformation(
                "Job concluido. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType} DurationMs: {DurationMs} Status: {Status}",
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                output.ExecutionDurationMs,
                "COMPLETED");
        }
        catch (AnalysisOutputParsingException ex)
        {
            var errorMessage = Truncate(ex.RawOutput, MaxErrorMessageLength);
            await PublishFailureAsync(message, stopwatch.ElapsedMilliseconds, errorMessage, cancellationToken);
        }
        catch (GitCloneException)
        {
            await PublishFailureAsync(
                message,
                stopwatch.ElapsedMilliseconds,
                "Falha ao clonar repositorio. Verifique as credenciais.",
                cancellationToken);
        }
        catch (CopilotRequestException ex)
        {
            await PublishFailureAsync(message, stopwatch.ElapsedMilliseconds, ex.Message, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await PublishFailureAsync(
                message,
                stopwatch.ElapsedMilliseconds,
                "Tempo limite excedido para a analise.",
                cancellationToken);
        }
        catch (TimeoutException)
        {
            await PublishFailureAsync(
                message,
                stopwatch.ElapsedMilliseconds,
                "Tempo limite excedido para a analise.",
                cancellationToken);
        }
    }

    private Task PublishFailureAsync(
        AnalysisJobMessage message,
        long durationMs,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var result = new AnalysisResultMessage(
            message.JobId,
            message.RequestId,
            message.AnalysisType,
            "FAILED",
            OutputJson: "{}",
            DurationMs: durationMs,
            ErrorMessage: errorMessage);
        return PublishFailureResultAsync(message, result, durationMs, cancellationToken);
    }

    private async Task PublishFailureResultAsync(
        AnalysisJobMessage message,
        AnalysisResultMessage result,
        long durationMs,
        CancellationToken cancellationToken)
    {
        await _resultPublisher.PublishResultAsync(result, cancellationToken);
        _logger.LogWarning(
            "Job concluido com falha. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType} DurationMs: {DurationMs} Status: {Status}",
            message.JobId,
            message.RequestId,
            message.AnalysisType,
            durationMs,
            "FAILED");
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }
}