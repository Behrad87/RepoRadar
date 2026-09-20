using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsPath = customPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "RepoRadar");
            Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "config.json");
        }
    }

    public string GetSettingsFilePath() => _settingsPath;

    public async Task<WorkspaceSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaults = WorkspaceSettings.CreateDefault();
                await SaveSettingsAsync(defaults).ConfigureAwait(false);
                return defaults;
            }

            var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<WorkspaceSettings>(json, JsonOptions);
            return settings ?? WorkspaceSettings.CreateDefault();
        }
        catch
        {
            return WorkspaceSettings.CreateDefault();
        }
    }

    public async Task SaveSettingsAsync(WorkspaceSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
        }
        catch
        {
            // Logging or graceful fallback
        }
    }
}
