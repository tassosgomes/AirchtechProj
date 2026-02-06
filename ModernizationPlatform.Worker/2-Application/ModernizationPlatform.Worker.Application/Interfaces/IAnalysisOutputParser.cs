using ModernizationPlatform.Worker.Application.DTOs;

namespace ModernizationPlatform.Worker.Application.Interfaces;

public interface IAnalysisOutputParser
{
    AnalysisOutput Parse(string responseText);
}
