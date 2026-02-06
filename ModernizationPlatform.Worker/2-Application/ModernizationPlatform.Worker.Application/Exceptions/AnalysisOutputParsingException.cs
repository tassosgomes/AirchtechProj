namespace ModernizationPlatform.Worker.Application.Exceptions;

public sealed class AnalysisOutputParsingException : Exception
{
    public AnalysisOutputParsingException(string message, string rawOutput, Exception? innerException = null)
        : base(message, innerException)
    {
        RawOutput = rawOutput;
    }

    public string RawOutput { get; }
}
