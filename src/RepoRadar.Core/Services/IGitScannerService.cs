using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public interface IGitScannerService
{
    Task<List<string>> DiscoverRepositoriesAsync(
        IEnumerable<string> rootPaths,
        int maxDepth = 3,
        CancellationToken cancellationToken = default);

    Task<GitRepositoryInfo> InspectRepositoryAsync(
        string repoPath,
        CancellationToken cancellationToken = default);

    Task<(List<GitRepositoryInfo> Repositories, ScanStatistics Stats)> ScanAllAsync(
        IEnumerable<string> rootPaths,
        int maxDepth = 3,
        IProgress<(int current, int total, string currentRepo)>? progress = null,
        CancellationToken cancellationToken = default);
}
