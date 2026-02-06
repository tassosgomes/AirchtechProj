using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Application.Commands;
using ModernizationPlatform.Application.Configuration;
using ModernizationPlatform.Application.DTOs;
using ModernizationPlatform.Application.Interfaces;
using ModernizationPlatform.Domain.Entities;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Interfaces;

namespace ModernizationPlatform.Application.Services;

public sealed class OrchestrationService : IOrchestrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrchestrationStateStore _stateStore;
    private readonly OrchestrationOptions _options;
    private readonly SemaphoreSlim _parallelism;
    private readonly ILogger<OrchestrationService> _logger;

    public OrchestrationService(
        IServiceScopeFactory scopeFactory,
        OrchestrationStateStore stateStore,
        IOptions<OrchestrationOptions> options,
        ILogger<OrchestrationService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parallelism = new SemaphoreSlim(_options.MaxParallelRequests, _options.MaxParallelRequests);
    }

    public async Task<AnalysisRequest> CreateRequestAsync(CreateAnalysisCommand command, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var validator = scope.ServiceProvider.GetRequiredService<FluentValidation.IValidator<CreateAnalysisCommand>>();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new FluentValidation.ValidationException(validationResult.Errors);
        }

        var request = new AnalysisRequest(command.RepositoryUrl, command.Provider, command.SelectedTypes);
        await requestRepository.AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _stateStore.StoreAccessToken(request.Id, command.AccessToken);

        _logger.LogInformation(
            "Solicitacao criada e enfileirada. RequestId: {RequestId} Provider: {Provider}",
            request.Id,
            request.Provider);

        return request;
    }

    public async Task ProcessPendingRequestsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();

        var pending = await requestRepository.GetQueuedAsync(_options.MaxParallelRequests, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        var tasks = pending
            .Select(request => ProcessRequestAsync(request.Id, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks);
    }

    private async Task ProcessRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await _parallelism.WaitAsync(cancellationToken);

        try
        {
            var accessToken = _stateStore.GetAccessToken(requestId);

            var requestSnapshot = await StartDiscoveryAsync(requestId, cancellationToken);
            var sharedContext = await ExecuteDiscoveryAsync(requestSnapshot.Request, accessToken, cancellationToken);
            await StartAnalysisAsync(requestId, cancellationToken);

            var sharedContextPayloads = BuildSharedContextPayloads(sharedContext);

            foreach (var analysisType in requestSnapshot.SelectedTypes)
            {
                foreach (var payload in sharedContextPayloads)
                {
                    var succeeded = await ProcessAnalysisJobAsync(
                        requestSnapshot,
                        analysisType,
                        payload,
                        accessToken,
                        cancellationToken);

                    if (!succeeded)
                    {
                        await FailRequestAsync(requestId, "Falha ao concluir job", cancellationToken);
                        return;
                    }
                }
            }

            await StartConsolidationAsync(requestId, cancellationToken);
            await ExecuteConsolidationAsync(requestId, cancellationToken);
            await CompleteRequestAsync(requestId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar request {RequestId}", requestId);
            await FailRequestAsync(requestId, ex.Message, cancellationToken);
        }
        finally
        {
            _stateStore.ClearRequest(requestId);
            _parallelism.Release();
        }
    }

    private async Task<RequestSnapshot> StartDiscoveryAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Request {requestId} not found.");
        }

        request.StartDiscovery();
        requestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Request {RequestId} status -> {Status}", requestId, request.Status);

        return new RequestSnapshot(request);
    }

    private async Task<SharedContext> ExecuteDiscoveryAsync(
        AnalysisRequest request,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var discoveryService = scope.ServiceProvider.GetRequiredService<IDiscoveryService>();

        var sharedContext = await discoveryService.ExecuteDiscoveryAsync(request, accessToken, cancellationToken);

        _logger.LogInformation("Request {RequestId} discovery concluido", request.Id);

        return sharedContext;
    }

    private async Task StartAnalysisAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Request {requestId} not found.");
        }

        request.StartAnalysis();
        requestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Request {RequestId} status -> {Status}", requestId, request.Status);
    }

    private async Task<bool> ProcessAnalysisJobAsync(
        RequestSnapshot snapshot,
        AnalysisType analysisType,
        string sharedContextJson,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        var prompt = await GetPromptAsync(analysisType, cancellationToken);
        var jobId = await CreateJobAsync(snapshot.RequestId, analysisType, cancellationToken);
        var retryCount = 0;

        while (true)
        {
            var message = new AnalysisJobMessage(
                jobId,
                snapshot.RequestId,
                snapshot.RepositoryUrl,
                snapshot.Provider.ToString(),
                accessToken ?? string.Empty,
                sharedContextJson,
                prompt.Content,
                analysisType.ToString(),
                _options.JobTimeoutSeconds);

            _logger.LogInformation(
                "Publicando job {JobId} para {AnalysisType}. Tentativa {Attempt}",
                jobId,
                analysisType,
                retryCount + 1);

            await PublishJobAsync(message, cancellationToken);

            var result = await _stateStore.WaitForJobResultAsync(jobId, cancellationToken);
            _stateStore.ClearJobResult(jobId);

            if (IsStatus(result.Status, "COMPLETED"))
            {
                _logger.LogInformation(
                    "Job {JobId} concluido para {AnalysisType}",
                    jobId,
                    analysisType);
                _stateStore.ClearJobState(jobId);
                return true;
            }

            if (IsStatus(result.Status, "FAILED"))
            {
                retryCount = _stateStore.IncrementRetryCount(jobId);
                if (retryCount <= _options.MaxJobRetries)
                {
                    await ResetJobForRetryAsync(jobId, cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Job {JobId} falhou apos {Retries} tentativas.",
                    jobId,
                    retryCount);
                _stateStore.ClearJobState(jobId);
                return false;
            }

            _logger.LogWarning(
                "Status inesperado para job {JobId}: {Status}",
                jobId,
                result.Status);
            _stateStore.ClearJobState(jobId);
            return false;
        }
    }

    private async Task<Prompt> GetPromptAsync(AnalysisType analysisType, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var promptCatalog = scope.ServiceProvider.GetRequiredService<IPromptCatalogService>();

        var prompt = await promptCatalog.GetByAnalysisTypeAsync(analysisType, cancellationToken);
        if (prompt == null)
        {
            throw new InvalidOperationException($"Prompt not found for {analysisType}.");
        }

        return prompt;
    }

    private async Task<Guid> CreateJobAsync(Guid requestId, AnalysisType analysisType, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = new AnalysisJob(requestId, analysisType);
        await jobRepository.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return job.Id;
    }

    private async Task PublishJobAsync(AnalysisJobMessage message, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IJobPublisher>();
        await publisher.PublishJobAsync(message, cancellationToken);
    }

    private async Task ResetJobForRetryAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAnalysisJobRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found for retry.");
        }

        job.ResetForRetry();
        jobRepository.Update(job);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task StartConsolidationAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Request {requestId} not found.");
        }

        request.StartConsolidation();
        requestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Request {RequestId} status -> {Status}", requestId, request.Status);
    }

    private async Task ExecuteConsolidationAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var consolidationService = scope.ServiceProvider.GetRequiredService<IConsolidationService>();

        await consolidationService.ConsolidateAsync(requestId, cancellationToken);
    }

    private async Task CompleteRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            throw new InvalidOperationException($"Request {requestId} not found.");
        }

        request.Complete();
        requestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Request {RequestId} status -> {Status}", requestId, request.Status);
    }

    private async Task FailRequestAsync(Guid requestId, string reason, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var requestRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRequestRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var request = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request == null)
        {
            _logger.LogWarning("Request {RequestId} nao encontrado para falha.", requestId);
            return;
        }

        if (request.Status is RequestStatus.Completed or RequestStatus.Failed)
        {
            return;
        }

        request.Fail();
        requestRepository.Update(request);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Request {RequestId} marcado como FAILED. Motivo: {Reason}", requestId, reason);
    }

    private IReadOnlyList<string> BuildSharedContextPayloads(SharedContext sharedContext)
    {
        if (_options.FanOutDependencyThreshold <= 0
            || sharedContext.Dependencies.Count <= _options.FanOutDependencyThreshold)
        {
            return [JsonSerializer.Serialize(ToPayload(sharedContext, sharedContext.Dependencies), JsonOptions)];
        }

        var batches = new List<string>();
        var batchSize = Math.Max(1, _options.FanOutDependencyBatchSize);
        var dependencies = sharedContext.Dependencies;

        for (var i = 0; i < dependencies.Count; i += batchSize)
        {
            var batch = dependencies.Skip(i).Take(batchSize).ToList();
            var payload = ToPayload(sharedContext, batch);
            batches.Add(JsonSerializer.Serialize(payload, JsonOptions));
        }

        return batches;
    }

    private static SharedContextPayload ToPayload(SharedContext context, IReadOnlyList<string> dependencies)
    {
        return new SharedContextPayload(
            context.RequestId,
            context.Version,
            context.Languages,
            context.Frameworks,
            dependencies,
            context.DirectoryStructureJson);
    }

    private static bool IsStatus(string? status, string expected)
    {
        return string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RequestSnapshot(AnalysisRequest Request)
    {
        public Guid RequestId => Request.Id;
        public string RepositoryUrl => Request.RepositoryUrl;
        public SourceProvider Provider => Request.Provider;
        public IReadOnlyList<AnalysisType> SelectedTypes => Request.SelectedTypes;
    }

    private sealed record SharedContextPayload(
        Guid RequestId,
        int Version,
        IReadOnlyList<string> Languages,
        IReadOnlyList<string> Frameworks,
        IReadOnlyList<string> Dependencies,
        string DirectoryStructureJson);
}
