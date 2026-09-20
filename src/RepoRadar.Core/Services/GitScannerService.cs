using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public class GitScannerService : IGitScannerService
{
    private readonly IGitProcessRunner _gitRunner;
    private readonly IGitParser _parser;

    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        "bin",
        "obj",
        ".vs",
        ".idea",
        ".vscode",
        "packages",
        "vendor",
        "target",
        "dist",
        "build",
        ".next",
        ".nuget",
        ".git"
    };

    public GitScannerService(IGitProcessRunner gitRunner, IGitParser parser)
    {
        _gitRunner = gitRunner;
        _parser = parser;
    }

    public async Task<List<string>> DiscoverRepositoriesAsync(
        IEnumerable<string> rootPaths,
        int maxDepth = 3,
        CancellationToken cancellationToken = default)
    {
        var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            foreach (var root in rootPaths)
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (!Directory.Exists(root)) continue;

                ScanDirectoryRecursive(new DirectoryInfo(root), 0, maxDepth, discovered, cancellationToken);
            }
        }, cancellationToken).ConfigureAwait(false);

        return discovered.OrderBy(x => x).ToList();
    }

    private static void ScanDirectoryRecursive(
        DirectoryInfo currentDir,
        int currentDepth,
        int maxDepth,
        HashSet<string> discovered,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            // Check if current directory has a .git directory or file
            var gitPath = Path.Combine(currentDir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                lock (discovered)
                {
                    discovered.Add(currentDir.FullName);
                }
                // Once we find a repo, stop descending inside it
                return;
            }

            if (currentDepth >= maxDepth) return;

            foreach (var subDir in currentDir.EnumerateDirectories())
            {
                if (cancellationToken.IsCancellationRequested) return;

                // Skip ignored directories (e.g. node_modules, bin, obj, .vs)
                if (IgnoredDirectoryNames.Contains(subDir.Name)) continue;
                if ((subDir.Attributes & FileAttributes.Hidden) != 0 && subDir.Name.StartsWith('.')) continue;

                ScanDirectoryRecursive(subDir, currentDepth + 1, maxDepth, discovered, cancellationToken);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip folders we do not have permission to read
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was moved or deleted
        }
        catch (Exception)
        {
            // Ignore other filesystem issues
        }
    }

    public async Task<GitRepositoryInfo> InspectRepositoryAsync(
        string repoPath,
        CancellationToken cancellationToken = default)
    {
        var repoInfo = new GitRepositoryInfo
        {
            Path = repoPath,
            Name = Path.GetFileName(repoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            LastScannedAt = DateTime.UtcNow
        };

        try
        {
            // 1. Run git status --porcelain=v2 --branch
            var statusResult = await _gitRunner.RunGitAsync(
                repoPath,
                "status --porcelain=v2 --branch",
                cancellationToken).ConfigureAwait(false);

            if (!statusResult.Success)
            {
                repoInfo.LastError = string.IsNullOrWhiteSpace(statusResult.StandardError)
                    ? "git status failed"
                    : statusResult.StandardError.Trim();
                return repoInfo;
            }

            _parser.ParseStatusPorcelainV2(statusResult.StandardOutput, repoInfo);

            // 2. Run git stash list
            var stashResult = await _gitRunner.RunGitAsync(
                repoPath,
                "stash list",
                cancellationToken).ConfigureAwait(false);

            if (stashResult.Success)
            {
                repoInfo.StashCount = _parser.ParseStashCount(stashResult.StandardOutput);
            }

            // 3. Run git log -1
            var logResult = await _gitRunner.RunGitAsync(
                repoPath,
                "log -1 --format=\"%h|%an|%cr|%s\"",
                cancellationToken).ConfigureAwait(false);

            if (logResult.Success)
            {
                _parser.ParseLastCommit(logResult.StandardOutput, repoInfo);
            }

            // 4. Run git config --get remote.origin.url
            var remoteResult = await _gitRunner.RunGitAsync(
                repoPath,
                "config --get remote.origin.url",
                cancellationToken).ConfigureAwait(false);

            if (remoteResult.Success)
            {
                repoInfo.RemoteUrl = _parser.NormalizeRemoteUrlToWebUrl(remoteResult.StandardOutput);
            }
        }
        catch (Exception ex)
        {
            repoInfo.LastError = ex.Message;
        }

        return repoInfo;
    }

    public async Task<(List<GitRepositoryInfo> Repositories, ScanStatistics Stats)> ScanAllAsync(
        IEnumerable<string> rootPaths,
        int maxDepth = 3,
        IProgress<(int current, int total, string currentRepo)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var repoPaths = await DiscoverRepositoriesAsync(rootPaths, maxDepth, cancellationToken).ConfigureAwait(false);
        var results = new ConcurrentBag<GitRepositoryInfo>();
        int total = repoPaths.Count;
        int processedCount = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 2, 8),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(repoPaths, parallelOptions, async (path, ct) =>
        {
            var info = await InspectRepositoryAsync(path, ct).ConfigureAwait(false);
            results.Add(info);

            var current = Interlocked.Increment(ref processedCount);
            progress?.Report((current, total, info.Name));
        }).ConfigureAwait(false);

        var sortedRepos = results.OrderBy(r => r.Name).ToList();

        var stats = new ScanStatistics
        {
            TotalRepositories = sortedRepos.Count,
            DirtyRepositories = sortedRepos.Count(r => r.IsDirty),
            CleanRepositories = sortedRepos.Count(r => r.IsClean),
            TotalUncommittedFiles = sortedRepos.Sum(r => r.ModifiedCount + r.UntrackedCount),
            TotalUnpushedCommits = sortedRepos.Sum(r => r.AheadCount),
            TotalStashes = sortedRepos.Sum(r => r.StashCount),
            LastScanTime = DateTime.UtcNow
        };

        return (sortedRepos, stats);
    }
}
