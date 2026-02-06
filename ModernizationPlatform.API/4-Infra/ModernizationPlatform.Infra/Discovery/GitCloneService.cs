using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Domain.Enums;
using ModernizationPlatform.Domain.Services;
using Polly;
using Polly.Retry;

namespace ModernizationPlatform.Infra.Discovery;

public class GitCloneService : IGitCloneService
{
    private readonly ILogger<GitCloneService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly TimeSpan _cloneTimeout;

    public GitCloneService(ILogger<GitCloneService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Read timeout from configuration, default to 10 minutes
        var timeoutMinutes = configuration.GetValue<int>("Discovery:CloneTimeoutMinutes", 10);
        _cloneTimeout = TimeSpan.FromMinutes(timeoutMinutes);

        // Configure retry policy: 3 attempts with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Git clone attempt {RetryCount} failed. Waiting {Delay}s before next retry",
                        retryCount, timeSpan.TotalSeconds);
                });
    }

    public async Task<string> CloneRepositoryAsync(
        string repositoryUrl,
        SourceProvider provider,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            throw new ArgumentException("Repository URL cannot be empty", nameof(repositoryUrl));
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"repo_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        _logger.LogInformation("Starting git clone to {TempPath}", tempPath);

        try
        {
            return await _retryPolicy.ExecuteAsync(async ct =>
            {
                var authenticatedUrl = BuildAuthenticatedUrl(repositoryUrl, provider, accessToken);
                await ExecuteGitCloneAsync(authenticatedUrl, tempPath, ct);
                return tempPath;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone repository after all retry attempts");
            CleanupRepository(tempPath);
            throw;
        }
    }

    public void CleanupRepository(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath) || !Directory.Exists(repositoryPath))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Cleaning up repository at {Path}", repositoryPath);
            
            // Make files writable before deletion (git files might be read-only)
            SetDirectoryWritable(repositoryPath);
            
            Directory.Delete(repositoryPath, recursive: true);
            
            _logger.LogInformation("Repository cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup repository at {Path}", repositoryPath);
        }
    }

    private string BuildAuthenticatedUrl(string repositoryUrl, SourceProvider provider, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogInformation("No access token provided, attempting public clone");
            return repositoryUrl;
        }

        try
        {
            var uri = new Uri(repositoryUrl);
            var authenticatedUri = provider switch
            {
                SourceProvider.GitHub => new UriBuilder(uri)
                {
                    UserName = accessToken,
                    Password = "x-oauth-basic"
                }.Uri,
                SourceProvider.AzureDevOps => new UriBuilder(uri)
                {
                    UserName = "pat",
                    Password = accessToken
                }.Uri,
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };

            // Never log the authenticated URL
            _logger.LogInformation("Built authenticated URL for {Provider}", provider);
            return authenticatedUri.ToString();
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid repository URL format");
            throw new ArgumentException("Invalid repository URL format", nameof(repositoryUrl), ex);
        }
    }

    private async Task ExecuteGitCloneAsync(string repositoryUrl, string targetPath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_cloneTimeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 --single-branch \"{repositoryUrl}\" \"{targetPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                output.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                error.AppendLine(args.Data);
            }
        };

        _logger.LogDebug("Executing: git clone");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore kill errors
            }

            throw new TimeoutException($"Git clone operation timed out after {_cloneTimeout.TotalMinutes} minutes");
        }

        if (process.ExitCode != 0)
        {
            var sanitizedError = SanitizeGitOutput(error.ToString());
            _logger.LogError("Git clone failed with exit code {ExitCode}: {Error}",
                process.ExitCode, sanitizedError);
            throw new InvalidOperationException($"Git clone failed: {sanitizedError}");
        }

        _logger.LogInformation("Git clone completed successfully");
    }

    private static string SanitizeGitOutput(string output)
    {
        // Remove any potential credentials from git output
        // Common patterns: https://token@github.com, https://user:pass@domain.com
        return System.Text.RegularExpressions.Regex.Replace(
            output,
            @"https://[^@]+@",
            "https://***@",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static void SetDirectoryWritable(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        
        foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                file.Attributes &= ~FileAttributes.ReadOnly;
            }
            catch
            {
                // Ignore individual file errors
            }
        }
    }
}
