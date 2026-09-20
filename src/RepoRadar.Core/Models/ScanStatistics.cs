using System;

namespace RepoRadar.Core.Models;

public class ScanStatistics
{
    public int TotalRepositories { get; set; }
    public int DirtyRepositories { get; set; }
    public int CleanRepositories { get; set; }
    public int TotalUncommittedFiles { get; set; }
    public int TotalUnpushedCommits { get; set; }
    public int TotalStashes { get; set; }
    public DateTime LastScanTime { get; set; } = DateTime.UtcNow;
}
