using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RepoRadar.Core.Services;

public class GitProcessRunner : IGitProcessRunner
{
    private readonly TimeSpan _defaultTimeout;

    public GitProcessRunner(TimeSpan? defaultTimeout = null)
    {
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(6);
    }

    public async Task<GitProcessResult> RunGitAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(_defaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Optional: avoid git pager / hooks / terminal prompts
            startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
            startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            if (!process.Start())
            {
                return GitProcessResult.Failed("Failed to start git process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore process kill errors
                }

                if (timeoutCts.IsCancellationRequested)
                {
                    return GitProcessResult.Failed($"Git command timed out after {_defaultTimeout.TotalSeconds} seconds.");
                }

                throw;
            }

            var stdout = outputBuilder.ToString();
            var stderr = errorBuilder.ToString();
            var exitCode = process.ExitCode;

            return new GitProcessResult(exitCode, stdout, stderr, exitCode == 0);
        }
        catch (Exception ex)
        {
            return GitProcessResult.Failed(ex.Message);
        }
    }
}
