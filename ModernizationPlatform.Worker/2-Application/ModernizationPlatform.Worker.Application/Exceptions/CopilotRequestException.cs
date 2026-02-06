namespace ModernizationPlatform.Worker.Application.Exceptions;

public sealed class CopilotRequestException : Exception
{
    public CopilotRequestException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
