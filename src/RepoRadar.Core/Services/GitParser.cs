using System;
using System.IO;
using System.Text.RegularExpressions;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public class GitParser : IGitParser
{
    private static readonly Regex AheadBehindRegex = new(@"#\s+branch\.ab\s+\+(\d+)\s+-(\d+)", RegexOptions.Compiled);
    private static readonly Regex BranchHeadRegex = new(@"#\s+branch\.head\s+(.+)", RegexOptions.Compiled);
    private static readonly Regex BranchUpstreamRegex = new(@"#\s+branch\.upstream\s+(.+)", RegexOptions.Compiled);

    public void ParseStatusPorcelainV2(string porcelainOutput, GitRepositoryInfo repoInfo)
    {
        if (string.IsNullOrWhiteSpace(porcelainOutput))
        {
            repoInfo.ModifiedCount = 0;
            repoInfo.UntrackedCount = 0;
            repoInfo.AheadCount = 0;
            repoInfo.BehindCount = 0;
            return;
        }

        int modifiedCount = 0;
        int untrackedCount = 0;
        int aheadCount = 0;
        int behindCount = 0;
        string branch = "HEAD";
        string? upstream = null;

        using var reader = new StringReader(porcelainOutput);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("# "))
            {
                var headMatch = BranchHeadRegex.Match(line);
                if (headMatch.Success)
                {
                    branch = headMatch.Groups[1].Value.Trim();
                    continue;
                }

                var upstreamMatch = BranchUpstreamRegex.Match(line);
                if (upstreamMatch.Success)
                {
                    upstream = upstreamMatch.Groups[1].Value.Trim();
                    continue;
                }

                var abMatch = AheadBehindRegex.Match(line);
                if (abMatch.Success)
                {
                    int.TryParse(abMatch.Groups[1].Value, out aheadCount);
                    int.TryParse(abMatch.Groups[2].Value, out behindCount);
                    continue;
                }

                continue;
            }

            // Normal changed entries: 1 <XY> ...
            // Renamed/copied entries: 2 <XY> ...
            // Unmerged entries: u <XY> ...
            if (line.StartsWith("1 ") || line.StartsWith("2 ") || line.StartsWith("u "))
            {
                modifiedCount++;
            }
            else if (line.StartsWith("? "))
            {
                untrackedCount++;
            }
        }

        repoInfo.CurrentBranch = branch;
        repoInfo.UpstreamBranch = upstream;
        repoInfo.AheadCount = aheadCount;
        repoInfo.BehindCount = behindCount;
        repoInfo.ModifiedCount = modifiedCount;
        repoInfo.UntrackedCount = untrackedCount;
    }

    public int ParseStashCount(string stashOutput)
    {
        if (string.IsNullOrWhiteSpace(stashOutput))
            return 0;

        int count = 0;
        using var reader = new StringReader(stashOutput);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                count++;
            }
        }

        return count;
    }

    public void ParseLastCommit(string logOutput, GitRepositoryInfo repoInfo)
    {
        if (string.IsNullOrWhiteSpace(logOutput))
            return;

        var parts = logOutput.Trim().Split('|', 4);
        if (parts.Length >= 1) repoInfo.LastCommitHash = parts[0];
        if (parts.Length >= 2) repoInfo.LastCommitAuthor = parts[1];
        if (parts.Length >= 3) repoInfo.LastCommitRelativeTime = parts[2];
        if (parts.Length >= 4) repoInfo.LastCommitMessage = parts[3];
    }

    public string? NormalizeRemoteUrlToWebUrl(string? rawRemoteUrl)
    {
        if (string.IsNullOrWhiteSpace(rawRemoteUrl))
            return null;

        var url = rawRemoteUrl.Trim();

        // Convert git@github.com:user/repo.git -> https://github.com/user/repo
        if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(url, @"git@([^:]+):(.+)");
            if (match.Success)
            {
                var host = match.Groups[1].Value;
                var repoPath = match.Groups[2].Value;
                url = $"https://{host}/{repoPath}";
            }
        }
        else if (url.StartsWith("ssh://git@", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(url, @"ssh://git@([^/]+)/(.+)");
            if (match.Success)
            {
                var host = match.Groups[1].Value;
                var repoPath = match.Groups[2].Value;
                url = $"https://{host}/{repoPath}";
            }
        }

        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            url = url.Substring(0, url.Length - 4);
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return null;
    }
}
