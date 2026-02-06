using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModernizationPlatform.Worker.Application.Exceptions;
using ModernizationPlatform.Worker.Application.Interfaces;

namespace ModernizationPlatform.Worker.Infra.CopilotSdk;

public sealed class GitCloneService : IGitCloneService
{
    private readonly ILogger<GitCloneService> _logger;
    private readonly TimeSpan _cloneTimeout;

    public GitCloneService(ILogger<GitCloneService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var timeoutMinutes = configuration.GetValue<int>("Worker:CloneTimeoutMinutes", 10);
        _cloneTimeout = TimeSpan.FromMinutes(timeoutMinutes);
    }

    public async Task<string> CloneRepositoryAsync(
        string repositoryUrl,
        string provider,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            throw new GitCloneException("Repository URL nao pode ser vazia");
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"worker_repo_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);

        _logger.LogInformation("Iniciando clone do repositorio");

        try
        {
            var authenticatedUrl = BuildAuthenticatedUrl(repositoryUrl, provider, accessToken);

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await ExecuteGitCloneAsync(authenticatedUrl, tempPath, cancellationToken);
                    return tempPath;
                }
                catch (Exception ex) when (attempt < 3)
                {
                    _logger.LogWarning(ex, "Tentativa {Attempt} de clone falhou. Tentando novamente.", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }

            throw new GitCloneException("Falha ao clonar repositorio");
        }
        catch (GitCloneException)
        {
            CleanupRepository(tempPath);
            throw;
        }
        catch (Exception ex)
        {
            CleanupRepository(tempPath);
            throw new GitCloneException("Falha ao clonar repositorio", ex);
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
            SetDirectoryWritable(repositoryPath);
            Directory.Delete(repositoryPath, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao limpar repositorio temporario");
        }
    }

    private string BuildAuthenticatedUrl(string repositoryUrl, string provider, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return repositoryUrl;
        }

        try
        {
            var uri = new Uri(repositoryUrl);
            var normalizedProvider = NormalizeProvider(provider);

            var authenticatedUri = normalizedProvider switch
            {
                "GitHub" => new UriBuilder(uri)
                {
                    UserName = accessToken,
                    Password = "x-oauth-basic"
                }.Uri,
                "AzureDevOps" => new UriBuilder(uri)
                {
                    UserName = "pat",
                    Password = accessToken
                }.Uri,
                _ => throw new GitCloneException("Provider nao suportado")
            };

            return authenticatedUri.ToString();
        }
        catch (UriFormatException ex)
        {
            throw new GitCloneException("URL de repositorio invalida", ex);
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
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                error.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            throw new TimeoutException("Timeout no clone do repositorio");
        }

        if (process.ExitCode != 0)
        {
            var sanitizedError = SanitizeGitOutput(error.ToString());
            throw new GitCloneException($"Git clone falhou: {sanitizedError}");
        }
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.Equals(provider, "GitHub", StringComparison.OrdinalIgnoreCase))
        {
            return "GitHub";
        }

        if (string.Equals(provider, "AzureDevOps", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Azure DevOps", StringComparison.OrdinalIgnoreCase))
        {
            return "AzureDevOps";
        }

        throw new GitCloneException("Provider nao suportado");
    }

    private static string SanitizeGitOutput(string output)
    {
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
            }
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }
}
