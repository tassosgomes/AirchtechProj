namespace ModernizationPlatform.Worker.Application.Exceptions;

public sealed class GitCloneException : Exception
{
    public GitCloneException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
