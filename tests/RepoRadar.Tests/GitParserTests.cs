using RepoRadar.Core.Models;
using RepoRadar.Core.Services;
using Xunit;

namespace RepoRadar.Tests;

public class GitParserTests
{
    private readonly GitParser _parser = new();

    [Fact]
    public void ParseStatusPorcelainV2_EmptyOutput_ZeroesCounts()
    {
        var repo = new GitRepositoryInfo();
        _parser.ParseStatusPorcelainV2(string.Empty, repo);

        Assert.Equal(0, repo.ModifiedCount);
        Assert.Equal(0, repo.UntrackedCount);
        Assert.Equal(0, repo.AheadCount);
        Assert.Equal(0, repo.BehindCount);
    }

    [Fact]
    public void ParseStatusPorcelainV2_CleanBranch_ExtractsBranchName()
    {
        var output = @"# branch.oid 9f8a3c4b1234567890abcdef1234567890abcdef
# branch.head feature/user-auth
# branch.upstream origin/feature/user-auth
# branch.ab +0 -0
";
        var repo = new GitRepositoryInfo();
        _parser.ParseStatusPorcelainV2(output, repo);

        Assert.Equal("feature/user-auth", repo.CurrentBranch);
        Assert.Equal("origin/feature/user-auth", repo.UpstreamBranch);
        Assert.Equal(0, repo.AheadCount);
        Assert.Equal(0, repo.BehindCount);
        Assert.Equal(0, repo.ModifiedCount);
        Assert.Equal(0, repo.UntrackedCount);
        Assert.True(repo.IsClean);
    }

    [Fact]
    public void ParseStatusPorcelainV2_AheadBehindAndDirtyFiles_ParsesAccurately()
    {
        var output = @"# branch.oid 1234567890abcdef1234567890abcdef12345678
# branch.head main
# branch.upstream origin/main
# branch.ab +3 -1
1 .M N... 100644 100644 100644 1234567 1234567 src/file1.cs
1 M. N... 100644 100644 100644 1234567 1234567 src/file2.cs
2 R. N... 100644 100644 100644 1234567 1234567 R100 old.cs new.cs
u UU N... 100644 100644 100644 100644 1234567 conflict.cs
? tests/newtest.cs
? docs/readme.txt
";
        var repo = new GitRepositoryInfo();
        _parser.ParseStatusPorcelainV2(output, repo);

        Assert.Equal("main", repo.CurrentBranch);
        Assert.Equal(3, repo.AheadCount);
        Assert.Equal(1, repo.BehindCount);
        Assert.Equal(4, repo.ModifiedCount); // 2 ordinary + 1 rename + 1 unmerged
        Assert.Equal(2, repo.UntrackedCount);
        Assert.True(repo.IsDirty);
        Assert.True(repo.HasUnpushed);
        Assert.True(repo.HasUncommitted);
    }

    [Fact]
    public void ParseStashCount_MultipleStashes_ReturnsCorrectCount()
    {
        var output = @"stash@{0}: WIP on main: 9f8a3c4 Commit msg
stash@{1}: On feature/auth: Work in progress
stash@{2}: WIP on dev: Temp stash
";
        var count = _parser.ParseStashCount(output);
        Assert.Equal(3, count);
    }

    [Fact]
    public void ParseStashCount_Empty_ReturnsZero()
    {
        Assert.Equal(0, _parser.ParseStashCount(string.Empty));
        Assert.Equal(0, _parser.ParseStashCount("   \r\n   "));
    }

    [Fact]
    public void ParseLastCommit_ValidLog_ParsesParts()
    {
        var log = "a1b2c3d|Behrad|15 minutes ago|feat: add repo radar dashboard\n";
        var repo = new GitRepositoryInfo();

        _parser.ParseLastCommit(log, repo);

        Assert.Equal("a1b2c3d", repo.LastCommitHash);
        Assert.Equal("Behrad", repo.LastCommitAuthor);
        Assert.Equal("15 minutes ago", repo.LastCommitRelativeTime);
        Assert.Equal("feat: add repo radar dashboard", repo.LastCommitMessage);
    }

    [Theory]
    [InlineData("git@github.com:Behrad87/RepoRadar.git", "https://github.com/Behrad87/RepoRadar")]
    [InlineData("https://github.com/Behrad87/RepoRadar.git", "https://github.com/Behrad87/RepoRadar")]
    [InlineData("https://gitlab.com/company/repo.git", "https://gitlab.com/company/repo")]
    [InlineData("ssh://git@github.com/Behrad87/RepoRadar.git", "https://github.com/Behrad87/RepoRadar")]
    public void NormalizeRemoteUrlToWebUrl_SupportedFormats_ConvertsProperly(string input, string expected)
    {
        var result = _parser.NormalizeRemoteUrlToWebUrl(input);
        Assert.Equal(expected, result);
    }
}
