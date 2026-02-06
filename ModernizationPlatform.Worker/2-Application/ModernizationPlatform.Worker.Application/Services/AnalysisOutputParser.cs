using System.Text.Json;
using System.Text.RegularExpressions;
using ModernizationPlatform.Worker.Application.DTOs;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Application.Services;

public sealed class AnalysisOutputParser : IAnalysisOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex JsonFenceRegex = new(
        "```(?:json)?\\s*(\\{[\\s\\S]*?\\})\\s*```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AnalysisOutput Parse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new AnalysisOutputParsingException("Resposta vazia do Copilot SDK", responseText);
        }

        var jsonPayload = ExtractJson(responseText);

        try
        {
            var output = JsonSerializer.Deserialize<AnalysisOutput>(jsonPayload, JsonOptions);
            if (output is null)
            {
                throw new AnalysisOutputParsingException("Falha ao desserializar resposta do Copilot SDK", responseText);
            }

            ValidateOutput(output, responseText);
            return output;
        }
        catch (JsonException ex)
        {
            throw new AnalysisOutputParsingException("JSON invalido retornado pelo Copilot SDK", responseText, ex);
        }
    }

    private static string ExtractJson(string responseText)
    {
        var fencedMatch = JsonFenceRegex.Match(responseText);
        if (fencedMatch.Success && fencedMatch.Groups.Count > 1)
        {
            return fencedMatch.Groups[1].Value;
        }

        var firstBrace = responseText.IndexOf('{');
        var lastBrace = responseText.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return responseText.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        throw new AnalysisOutputParsingException("Nenhum JSON encontrado na resposta do Copilot SDK", responseText);
    }

    private static void ValidateOutput(AnalysisOutput output, string rawOutput)
    {
        if (output.Findings is null)
        {
            throw new AnalysisOutputParsingException("Campo 'findings' ausente na resposta", rawOutput);
        }

        if (output.Metadata is null || string.IsNullOrWhiteSpace(output.Metadata.AnalysisType))
        {
            throw new AnalysisOutputParsingException("Campo 'metadata' invalido na resposta", rawOutput);
        }

        foreach (var finding in output.Findings)
        {
            if (string.IsNullOrWhiteSpace(finding.Title) || string.IsNullOrWhiteSpace(finding.Severity))
            {
                throw new AnalysisOutputParsingException("Achado com campos obrigatorios ausentes", rawOutput);
            }
        }
    }
}
