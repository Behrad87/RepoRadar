using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepoRadar.App.Services;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;

namespace RepoRadar.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGitScannerService _scannerService;
    private readonly IGitProcessRunner _gitRunner;
    private readonly ISettingsService _settingsService;
    private readonly ILauncherService _launcherService;

    private DispatcherTimer? _scanTimer;
    private DateTime _lastAlertDate = DateTime.MinValue;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private WorkspaceSettings _settings = new();

    [ObservableProperty]
    private ObservableCollection<RepoItemViewModel> _allRepositories = new();

    [ObservableProperty]
    private ObservableCollection<RepoItemViewModel> _displayRepositories = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _filterOnlyDirty = true;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "Ready to scan";

    [ObservableProperty]
    private string _scanProgressText = string.Empty;

    [ObservableProperty]
    private string _lastScannedText = "Not scanned yet";

    [ObservableProperty]
    private int _totalReposCount;

    [ObservableProperty]
    private int _dirtyReposCount;

    [ObservableProperty]
    private int _cleanReposCount;

    [ObservableProperty]
    private int _totalUnpushedCount;

    [ObservableProperty]
    private int _totalUncommittedCount;

    [ObservableProperty]
    private int _totalStashesCount;

    [ObservableProperty]
    private bool _hasRepositories;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private SettingsViewModel? _settingsVm;

    public TrayIconManager? TrayManager { get; set; }

    public MainViewModel(
        IGitScannerService scannerService,
        IGitProcessRunner gitRunner,
        ISettingsService settingsService,
        ILauncherService launcherService)
    {
        _scannerService = scannerService;
        _gitRunner = gitRunner;
        _settingsService = settingsService;
        _launcherService = launcherService;
    }

    // Default constructor for XAML designer
    public MainViewModel()
    {
        _gitRunner = new GitProcessRunner();
        var parser = new GitParser();
        _scannerService = new GitScannerService(_gitRunner, parser);
        _settingsService = new SettingsService();
        _launcherService = new LauncherService();
    }

    public async Task InitializeAsync()
    {
        Settings = await _settingsService.LoadSettingsAsync();
        FilterOnlyDirty = Settings.FilterOnlyDirty;

        InitTimer();

        // Perform initial scan
        await ScanAsync();
    }

    private void InitTimer()
    {
        _scanTimer?.Stop();

        if (Settings.AutoScanEnabled && Settings.ScanIntervalMinutes > 0)
        {
            _scanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(Settings.ScanIntervalMinutes)
            };
            _scanTimer.Tick += async (s, e) =>
            {
                await ScanAsync();
                CheckEndOfDayAlert();
            };
            _scanTimer.Start();
        }
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        if (IsScanning) return;

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        IsScanning = true;
        StatusText = "Scanning repositories...";
        ScanProgressText = "Discovering .git folders...";

        try
        {
            var progress = new Progress<(int current, int total, string currentRepo)>(report =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ScanProgressText = $"Analyzing {report.current}/{report.total}: {report.currentRepo}";
                });
            });

            var (repos, stats) = await _scannerService.ScanAllAsync(
                Settings.WorkspaceFolders,
                Settings.MaxScanDepth,
                progress,
                _scanCts.Token);

            AllRepositories.Clear();
            foreach (var repo in repos)
            {
                var vm = new RepoItemViewModel(
                    repo,
                    _launcherService,
                    _scannerService,
                    _gitRunner,
                    OnSingleRepoUpdated);
                AllRepositories.Add(vm);
            }

            UpdateStats(stats);
            ApplyFilter();

            LastScannedText = $"Last scan: {DateTime.Now:HH:mm:ss}";
            StatusText = $"Scan complete. {stats.TotalRepositories} repos detected.";

            TrayManager?.UpdateStatus(repos, Settings.PreferredEditor);

            CheckEndOfDayAlert();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanProgressText = string.Empty;
        }
    }

    private void OnSingleRepoUpdated(RepoItemViewModel item)
    {
        RecalculateStats();
        ApplyFilter();
        TrayManager?.UpdateStatus(AllRepositories.Select(r => r.Model).ToList(), Settings.PreferredEditor);
    }

    private void RecalculateStats()
    {
        TotalReposCount = AllRepositories.Count;
        DirtyReposCount = AllRepositories.Count(r => r.IsDirty);
        CleanReposCount = AllRepositories.Count(r => r.IsClean);
        TotalUnpushedCount = AllRepositories.Sum(r => r.AheadCount);
        TotalUncommittedCount = AllRepositories.Sum(r => r.ModifiedCount + r.UntrackedCount);
        TotalStashesCount = AllRepositories.Sum(r => r.StashCount);
    }

    private void UpdateStats(ScanStatistics stats)
    {
        TotalReposCount = stats.TotalRepositories;
        DirtyReposCount = stats.DirtyRepositories;
        CleanReposCount = stats.CleanRepositories;
        TotalUnpushedCount = stats.TotalUnpushedCommits;
        TotalUncommittedCount = stats.TotalUncommittedFiles;
        TotalStashesCount = stats.TotalStashes;
        HasRepositories = stats.TotalRepositories > 0;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnFilterOnlyDirtyChanged(bool value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = AllRepositories.AsEnumerable();

        if (FilterOnlyDirty)
        {
            filtered = filtered.Where(r => r.IsDirty);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                r.CurrentBranch.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        DisplayRepositories = new ObservableCollection<RepoItemViewModel>(filtered);
        HasNoResults = DisplayRepositories.Count == 0 && AllRepositories.Count > 0;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ToggleFilterOnlyDirty()
    {
        FilterOnlyDirty = !FilterOnlyDirty;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsVm = new SettingsViewModel(Settings, _settingsService, updated =>
        {
            Settings = updated;
            InitTimer();
            IsSettingsOpen = false;
            _ = ScanAsync();
        });
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void OpenDonationDialog()
    {
        var dialog = new Views.DonationDialog
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void OpenSponsors() => _launcherService.OpenInBrowser("https://github.com/sponsors/Behrad87");

    [RelayCommand]
    private void OpenKofi() => _launcherService.OpenInBrowser("https://ko-fi.com/behrad87");

    [RelayCommand]
    private void OpenReymit() => _launcherService.OpenInBrowser("https://reymit.ir/behrad87");

    [RelayCommand]
    private void OpenGitHub() => _launcherService.OpenInBrowser("https://github.com/Behrad87/RepoRadar");

    private void CheckEndOfDayAlert()
    {
        if (!Settings.NotifyOnDirtyBeforeEndOfDay) return;

        var now = DateTime.Now;
        if (now.Hour >= Settings.EndOfDayHour && _lastAlertDate.Date != now.Date)
        {
            if (DirtyReposCount > 0)
            {
                _lastAlertDate = now;
                TrayManager?.ShowEndOfDayAlert(DirtyReposCount, TotalUnpushedCount, TotalUncommittedCount);
            }
        }
    }
}
