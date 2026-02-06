using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModernizationPlatform.Worker.Application.Configuration;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Infra.CopilotSdk;

public sealed class CopilotClient : ICopilotClient
{
    private readonly HttpClient _httpClient;
    private readonly CopilotSdkOptions _options;
    private readonly ILogger<CopilotClient> _logger;

    public CopilotClient(
        HttpClient httpClient,
        IOptions<CopilotSdkOptions> options,
        ILogger<CopilotClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CopilotResponse> AnalyzeAsync(CopilotRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new CopilotRequestException("Copilot SDK endpoint nao configurado");
        }

        var payload = new CopilotSdkRequest(
            request.RepositorySnapshot,
            request.SharedContextJson,
            request.PromptContent,
            request.AnalysisType);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new CopilotRequestException($"Copilot SDK respondeu {response.StatusCode}: {Truncate(error, 1000)}");
            }

            var sdkResponse = await response.Content.ReadFromJsonAsync<CopilotSdkResponse>(cancellationToken: cancellationToken);
            if (sdkResponse is null || string.IsNullOrWhiteSpace(sdkResponse.Content))
            {
                throw new CopilotRequestException("Resposta do Copilot SDK sem conteudo");
            }

            return new CopilotResponse(sdkResponse.Content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao chamar o Copilot SDK");
            throw new CopilotRequestException("Falha ao chamar o Copilot SDK", ex);
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }

    private sealed record CopilotSdkRequest(
        string RepositorySnapshot,
        string SharedContextJson,
        string PromptContent,
        string AnalysisType);

    private sealed record CopilotSdkResponse(string Content);
}
