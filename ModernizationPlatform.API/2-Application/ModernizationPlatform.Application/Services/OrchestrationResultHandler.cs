using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class OrchestrationResultHandler : IAnalysisResultHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrchestrationStateStore _stateStore;
    private readonly ILogger<OrchestrationResultHandler> _logger;

    public OrchestrationResultHandler(
        IServiceScopeFactory scopeFactory,
        OrchestrationStateStore stateStore,
        ILogger<OrchestrationResultHandler> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(AnalysisResultMessage message, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await jobRepository.GetByIdAsync(message.JobId, cancellationToken);
        if (job == null)
        {
            _logger.LogWarning(
                "Resultado recebido para job inexistente. JobId: {JobId} RequestId: {RequestId}",
                message.JobId,
                message.RequestId);
            return;
        }

        if (IsStatus(message.Status, "RUNNING"))
        {
            if (job.Status == JobStatus.Pending)
            {
                job.Start();
                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (IsStatus(message.Status, "COMPLETED"))
        {
            if (job.Status != JobStatus.Completed)
            {
                job.Complete(message.OutputJson, TimeSpan.FromMilliseconds(message.DurationMs));
                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Job concluido. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType} DurationMs: {DurationMs} Status: {Status}",
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                message.DurationMs,
                message.Status);

            _stateStore.CompleteJobResult(message);
            return;
        }

        if (IsStatus(message.Status, "FAILED"))
        {
            if (job.Status != JobStatus.Failed)
            {
                var duration = message.DurationMs > 0
                    ? TimeSpan.FromMilliseconds(message.DurationMs)
                    : (TimeSpan?)null;

                job.Fail(message.OutputJson, duration);
                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogWarning(
                "Job concluido com falha. JobId: {JobId} RequestId: {RequestId} Type: {AnalysisType} DurationMs: {DurationMs} Status: {Status}",
                message.JobId,
                message.RequestId,
                message.AnalysisType,
                message.DurationMs,
                message.Status);

            _stateStore.CompleteJobResult(message);
            return;
        }

        _logger.LogWarning(
            "Status desconhecido recebido. JobId: {JobId} Status: {Status}",
            message.JobId,
            message.Status);
    }

    private static bool IsStatus(string? status, string expected)
    {
        return string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    }
}
