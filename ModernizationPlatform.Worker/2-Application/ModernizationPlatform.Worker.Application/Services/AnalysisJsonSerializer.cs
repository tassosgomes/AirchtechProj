using System.Text.Json;
using System.Text.Json.Serialization;
using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Services;

public static class AnalysisJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(AnalysisOutput output)
    {
        return JsonSerializer.Serialize(output, JsonOptions);
    }
}
