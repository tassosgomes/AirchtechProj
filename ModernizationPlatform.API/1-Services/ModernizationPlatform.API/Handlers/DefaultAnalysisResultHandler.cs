using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;

namespace ModernizationPlatform.API.Handlers;

public sealed class DefaultAnalysisResultHandler : IAnalysisResultHandler
{
    private readonly ILogger<DefaultAnalysisResultHandler> _logger;

    public DefaultAnalysisResultHandler(ILogger<DefaultAnalysisResultHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AnalysisResultMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resultado recebido. JobId: {JobId} RequestId: {RequestId} Status: {Status} Type: {AnalysisType}",
            message.JobId,
            message.RequestId,
            message.Status,
            message.AnalysisType);

        return Task.CompletedTask;
    }
}