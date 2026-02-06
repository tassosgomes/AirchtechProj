using Microsoft.Extensions.Logging;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Consumers;

public sealed class AnalysisJobHandler : IAnalysisJobHandler
{
    private readonly IResultPublisher _resultPublisher;
    private readonly ILogger<AnalysisJobHandler> _logger;

    public AnalysisJobHandler(IResultPublisher resultPublisher, ILogger<AnalysisJobHandler> logger)
    {
        _resultPublisher = resultPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Job recebido. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType}",
            message.JobId,
            message.RequestId,
            message.AnalysisType);

        var result = new AnalysisResultMessage(
            message.JobId,
            message.RequestId,
            message.AnalysisType,
            "FAILED",
            OutputJson: "{}",
            DurationMs: 0,
            ErrorMessage: "Executor de análise ainda não implementado");

        await _resultPublisher.PublishResultAsync(result, cancellationToken);
    }
}