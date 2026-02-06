using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Application.Services;

public sealed class AnalysisExecutor : IAnalysisExecutor
{
    private readonly IGitCloneService _gitCloneService;
    private readonly IRepositorySnapshotBuilder _snapshotBuilder;
    private readonly ICopilotClient _copilotClient;
    private readonly IAnalysisOutputParser _outputParser;
    private readonly ILogger<AnalysisExecutor> _logger;

    public AnalysisExecutor(
        IGitCloneService gitCloneService,
        IRepositorySnapshotBuilder snapshotBuilder,
        ICopilotClient copilotClient,
        IAnalysisOutputParser outputParser,
        ILogger<AnalysisExecutor> logger)
    {
        _gitCloneService = gitCloneService;
        _snapshotBuilder = snapshotBuilder;
        _copilotClient = copilotClient;
        _outputParser = outputParser;
        _logger = logger;
    }

    public async Task<AnalysisOutput> ExecuteAsync(AnalysisInput input, CancellationToken cancellationToken)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        string? repositoryPath = null;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Clonando repositorio para analise. Tipo: {AnalysisType}", input.AnalysisType);

            repositoryPath = await _gitCloneService.CloneRepositoryAsync(
                input.RepositoryUrl,
                input.Provider,
                input.AccessToken,
                cancellationToken);

            var snapshot = await _snapshotBuilder.BuildAsync(repositoryPath, cancellationToken);
            var request = new CopilotRequest(
                snapshot,
                input.SharedContextJson,
                input.PromptContent,
                input.AnalysisType);

            var response = await _copilotClient.AnalyzeAsync(request, cancellationToken);
            var parsedOutput = _outputParser.Parse(response.Content);

            var durationMs = (long)stopwatch.Elapsed.TotalMilliseconds;
            var metadata = new AnalysisMetadata(input.AnalysisType, parsedOutput.Findings.Count, durationMs);

            return new AnalysisOutput(parsedOutput.Findings, metadata, durationMs);
        }
        catch (GitCloneException)
        {
            throw;
        }
        catch (CopilotRequestException)
        {
            throw;
        }
        catch (AnalysisOutputParsingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha inesperada durante execucao da analise");
            throw;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(repositoryPath))
            {
                _gitCloneService.CleanupRepository(repositoryPath);
            }
        }
    }
}
