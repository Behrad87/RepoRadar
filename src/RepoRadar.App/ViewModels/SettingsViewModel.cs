using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;

namespace RepoRadar.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly Action<WorkspaceSettings> _onSettingsSaved;

    [ObservableProperty]
    private WorkspaceSettings _settings;

    [ObservableProperty]
    private ObservableCollection<string> _workspaceFolders = new();

    [ObservableProperty]
    private string? _selectedFolder;

    [ObservableProperty]
    private int _scanIntervalMinutes = 10;

    [ObservableProperty]
    private bool _autoScanEnabled = true;

    [ObservableProperty]
    private bool _filterOnlyDirty = true;

    [ObservableProperty]
    private bool _closeToTray = true;

    [ObservableProperty]
    private bool _startMinimized = false;

    [ObservableProperty]
    private bool _notifyOnDirtyBeforeEndOfDay = true;

    [ObservableProperty]
    private int _endOfDayHour = 18;

    [ObservableProperty]
    private EditorType _preferredEditor = EditorType.VSCode;

    public EditorType[] AvailableEditors => (EditorType[])Enum.GetValues(typeof(EditorType));
    public int[] AvailableIntervals => new[] { 2, 5, 10, 15, 30, 60 };

    public SettingsViewModel(
        WorkspaceSettings settings,
        ISettingsService settingsService,
        Action<WorkspaceSettings> onSettingsSaved)
    {
        _settings = settings;
        _settingsService = settingsService;
        _onSettingsSaved = onSettingsSaved;

        WorkspaceFolders = new ObservableCollection<string>(settings.WorkspaceFolders);
        ScanIntervalMinutes = settings.ScanIntervalMinutes;
        AutoScanEnabled = settings.AutoScanEnabled;
        FilterOnlyDirty = settings.FilterOnlyDirty;
        CloseToTray = settings.CloseToTray;
        StartMinimized = settings.StartMinimized;
        NotifyOnDirtyBeforeEndOfDay = settings.NotifyOnDirtyBeforeEndOfDay;
        EndOfDayHour = settings.EndOfDayHour;
        PreferredEditor = settings.PreferredEditor;
    }

    [RelayCommand]
    private void AddFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select Git Workspace Folder to Monitor",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            if (!WorkspaceFolders.Contains(dialog.SelectedPath))
            {
                WorkspaceFolders.Add(dialog.SelectedPath);
            }
        }
    }

    [RelayCommand]
    private void RemoveSelectedFolder()
    {
        if (!string.IsNullOrEmpty(SelectedFolder))
        {
            WorkspaceFolders.Remove(SelectedFolder);
            SelectedFolder = WorkspaceFolders.FirstOrDefault();
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        Settings.WorkspaceFolders = WorkspaceFolders.ToList();
        Settings.ScanIntervalMinutes = ScanIntervalMinutes;
        Settings.AutoScanEnabled = AutoScanEnabled;
        Settings.FilterOnlyDirty = FilterOnlyDirty;
        Settings.CloseToTray = CloseToTray;
        Settings.StartMinimized = StartMinimized;
        Settings.NotifyOnDirtyBeforeEndOfDay = NotifyOnDirtyBeforeEndOfDay;
        Settings.EndOfDayHour = EndOfDayHour;
        Settings.PreferredEditor = PreferredEditor;

        await _settingsService.SaveSettingsAsync(Settings);
        _onSettingsSaved(Settings);
    }
}
