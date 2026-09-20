using System;

namespace RepoRadar.Core.Models;

public class GitRepositoryInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string CurrentBranch { get; set; } = "HEAD";
    public string? UpstreamBranch { get; set; }
    public int AheadCount { get; set; }
    public int BehindCount { get; set; }
    public int ModifiedCount { get; set; }
    public int UntrackedCount { get; set; }
    public int StashCount { get; set; }
    public string? RemoteUrl { get; set; }
    public string? LastCommitHash { get; set; }
    public string? LastCommitAuthor { get; set; }
    public string? LastCommitMessage { get; set; }
    public string? LastCommitRelativeTime { get; set; }
    public string? LastError { get; set; }
    public DateTime LastScannedAt { get; set; } = DateTime.UtcNow;

    public bool HasError => !string.IsNullOrEmpty(LastError);
    public bool HasUncommitted => ModifiedCount > 0 || UntrackedCount > 0;
    public bool HasUnpushed => AheadCount > 0;
    public bool HasUnpulled => BehindCount > 0;
    public bool HasStashes => StashCount > 0;
    public bool IsDirty => HasUncommitted || HasUnpushed || HasStashes;
    public bool IsClean => !IsDirty && !HasError;

    public string StatusSummary
    {
        get
        {
            if (HasError) return $"Error: {LastError}";
            if (IsClean) return "Clean & Synchronized";

            var parts = new System.Collections.Generic.List<string>();
            if (ModifiedCount > 0) parts.Add($"{ModifiedCount} modified");
            if (UntrackedCount > 0) parts.Add($"{UntrackedCount} untracked");
            if (AheadCount > 0) parts.Add($"↑{AheadCount} unpushed");
            if (BehindCount > 0) parts.Add($"↓{BehindCount} behind");
            if (StashCount > 0) parts.Add($"📦{StashCount} stash{(StashCount > 1 ? "es" : "")}");

            return string.Join(" • ", parts);
        }
    }
}
