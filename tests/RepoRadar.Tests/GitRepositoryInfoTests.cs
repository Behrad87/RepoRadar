using RepoRadar.Core.Models;
using Xunit;

namespace RepoRadar.Tests;

public class GitRepositoryInfoTests
{
    [Fact]
    public void IsDirty_WhenModifiedFilesExist_ReturnsTrue()
    {
        var repo = new GitRepositoryInfo { ModifiedCount = 2 };
        Assert.True(repo.IsDirty);
        Assert.True(repo.HasUncommitted);
        Assert.False(repo.IsClean);
    }

    [Fact]
    public void IsDirty_WhenUnpushedCommitsExist_ReturnsTrue()
    {
        var repo = new GitRepositoryInfo { AheadCount = 1 };
        Assert.True(repo.IsDirty);
        Assert.True(repo.HasUnpushed);
        Assert.False(repo.IsClean);
    }

    [Fact]
    public void IsDirty_WhenStashesExist_ReturnsTrue()
    {
        var repo = new GitRepositoryInfo { StashCount = 1 };
        Assert.True(repo.IsDirty);
        Assert.True(repo.HasStashes);
        Assert.False(repo.IsClean);
    }

    [Fact]
    public void IsClean_WhenZeroCountsAndNoError_ReturnsTrue()
    {
        var repo = new GitRepositoryInfo
        {
            ModifiedCount = 0,
            UntrackedCount = 0,
            AheadCount = 0,
            BehindCount = 0,
            StashCount = 0,
            LastError = null
        };
        Assert.False(repo.IsDirty);
        Assert.True(repo.IsClean);
        Assert.Equal("Clean & Synchronized", repo.StatusSummary);
    }

    [Fact]
    public void StatusSummary_FormatsMultipleFlagsCorrectly()
    {
        var repo = new GitRepositoryInfo
        {
            ModifiedCount = 2,
            AheadCount = 1,
            StashCount = 3
        };

        var summary = repo.StatusSummary;
        Assert.Contains("2 modified", summary);
        Assert.Contains("↑1 unpushed", summary);
        Assert.Contains("📦3 stashes", summary);
    }
}
