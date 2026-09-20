using System;
using System.Collections.Generic;
using System.IO;

namespace RepoRadar.Core.Models;

public class WorkspaceSettings
{
    public List<string> WorkspaceFolders { get; set; } = new();
    public int ScanIntervalMinutes { get; set; } = 10;
    public bool AutoScanEnabled { get; set; } = true;
    public int MaxScanDepth { get; set; } = 3;
    public bool FilterOnlyDirty { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool NotifyOnDirtyBeforeEndOfDay { get; set; } = true;
    public int EndOfDayHour { get; set; } = 18;
    public EditorType PreferredEditor { get; set; } = EditorType.VSCode;

    public static WorkspaceSettings CreateDefault()
    {
        var settings = new WorkspaceSettings();

        // Check common developer workspace locations
        var commonPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Code"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "repos"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects"),
            @"D:\repos"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && !settings.WorkspaceFolders.Contains(path))
            {
                settings.WorkspaceFolders.Add(path);
                break;
            }
        }

        if (settings.WorkspaceFolders.Count == 0)
        {
            var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Directory.Exists(userHome))
            {
                settings.WorkspaceFolders.Add(userHome);
            }
        }

        return settings;
    }
}
