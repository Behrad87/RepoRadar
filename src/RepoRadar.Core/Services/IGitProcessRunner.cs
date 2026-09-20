using System.Threading;
using System.Threading.Tasks;

namespace RepoRadar.Core.Services;

public record GitProcessResult(int ExitCode, string StandardOutput, string StandardError, bool Success)
{
    public static GitProcessResult Failed(string error) => new(-1, string.Empty, error, false);
}

public interface IGitProcessRunner
{
    Task<GitProcessResult> RunGitAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default);
}
