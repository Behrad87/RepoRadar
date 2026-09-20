using System;
using System.IO;
using System.Threading.Tasks;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;
using Xunit;

namespace RepoRadar.Tests;

public class SettingsTests
{
    [Fact]
    public async Task SaveAndLoadSettings_PersistsAllProperties()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"RepoRadar_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);

            var settings = new WorkspaceSettings
            {
                WorkspaceFolders = new() { @"C:\MyRepos", @"D:\Projects" },
                ScanIntervalMinutes = 25,
                AutoScanEnabled = false,
                FilterOnlyDirty = false,
                PreferredEditor = EditorType.Rider,
                MaxScanDepth = 4,
                CloseToTray = true,
                StartMinimized = true,
                NotifyOnDirtyBeforeEndOfDay = true,
                EndOfDayHour = 17
            };

            await service.SaveSettingsAsync(settings);

            var loaded = await service.LoadSettingsAsync();

            Assert.Equal(2, loaded.WorkspaceFolders.Count);
            Assert.Contains(@"C:\MyRepos", loaded.WorkspaceFolders);
            Assert.Contains(@"D:\Projects", loaded.WorkspaceFolders);
            Assert.Equal(25, loaded.ScanIntervalMinutes);
            Assert.False(loaded.AutoScanEnabled);
            Assert.False(loaded.FilterOnlyDirty);
            Assert.Equal(EditorType.Rider, loaded.PreferredEditor);
            Assert.Equal(4, loaded.MaxScanDepth);
            Assert.True(loaded.CloseToTray);
            Assert.True(loaded.StartMinimized);
            Assert.True(loaded.NotifyOnDirtyBeforeEndOfDay);
            Assert.Equal(17, loaded.EndOfDayHour);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
