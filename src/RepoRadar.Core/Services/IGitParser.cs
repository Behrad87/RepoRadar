using System;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public interface IGitParser
{
    void ParseStatusPorcelainV2(string porcelainOutput, GitRepositoryInfo repoInfo);
    int ParseStashCount(string stashOutput);
    void ParseLastCommit(string logOutput, GitRepositoryInfo repoInfo);
    string? NormalizeRemoteUrlToWebUrl(string? rawRemoteUrl);
}
