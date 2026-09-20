using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RepoRadar.App.Services;
using RepoRadar.App.ViewModels;
using RepoRadar.App.Views;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;

namespace RepoRadar.App;

public partial class App : System.Windows.Application
{
    private TrayIconManager? _trayManager;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--screenshot"))
        {
            try
            {
                CaptureScreenshots();
            }
            catch (Exception ex)
            {
                var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                Directory.CreateDirectory(assetsDir);
                File.WriteAllText(Path.Combine(assetsDir, "screenshot_error.log"), ex.ToString());
            }
            Shutdown(0);
            return;
        }

        var gitRunner = new GitProcessRunner();
        var parser = new GitParser();
        var scannerService = new GitScannerService(gitRunner, parser);
        var settingsService = new SettingsService();
        var launcherService = new LauncherService();

        var viewModel = new MainViewModel(scannerService, gitRunner, settingsService, launcherService);
        _mainWindow = new MainWindow(viewModel);

        _trayManager = new TrayIconManager(
            launcherService,
            onShowDashboard: () => _mainWindow.ShowAndActivate(),
            onScanRequested: () => _ = viewModel.ScanAsync(),
            onShowSettings: () =>
            {
                viewModel.OpenSettingsCommand.Execute(null);
                _mainWindow.ShowAndActivate();
            },
            onExitRequested: () =>
            {
                _mainWindow.RequestExit();
                Shutdown();
            });

        viewModel.TrayManager = _trayManager;

        var settings = await settingsService.LoadSettingsAsync();
        if (!settings.StartMinimized)
        {
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        base.OnExit(e);
    }

    private void CaptureScreenshots()
    {
        var assetsDir = @"D:\repos\RepoRadar\assets";
        if (!Directory.Exists(assetsDir))
        {
            Directory.CreateDirectory(assetsDir);
        }

        var gitRunner = new GitProcessRunner();
        var parser = new GitParser();
        var scannerService = new GitScannerService(gitRunner, parser);
        var launcherService = new LauncherService();
        var settingsService = new SettingsService();

        var vm = new MainViewModel(scannerService, gitRunner, settingsService, launcherService)
        {
            TotalReposCount = 18,
            DirtyReposCount = 4,
            CleanReposCount = 14,
            TotalUnpushedCount = 5,
            TotalUncommittedCount = 7,
            TotalStashesCount = 3,
            FilterOnlyDirty = true,
            HasRepositories = true,
            LastScannedText = "Last scan: 18:25:10",
            StatusText = "Scan complete. 18 repositories monitored across 2 workspaces."
        };

        var mockRepos = new List<GitRepositoryInfo>
        {
            new()
            {
                Name = "RepoRadar",
                Path = @"D:\repos\RepoRadar",
                CurrentBranch = "main",
                UpstreamBranch = "origin/main",
                AheadCount = 2,
                ModifiedCount = 1,
                UntrackedCount = 1,
                StashCount = 1,
                RemoteUrl = "https://github.com/Behrad87/RepoRadar",
                LastCommitHash = "f71b2a9",
                LastCommitAuthor = "Behrad",
                LastCommitRelativeTime = "25 minutes ago",
                LastCommitMessage = "feat: add tray menu launcher and status pills"
            },
            new()
            {
                Name = "DevPurge",
                Path = @"D:\repos\DevPurge",
                CurrentBranch = "feat/analyzer",
                UpstreamBranch = "origin/feat/analyzer",
                AheadCount = 3,
                ModifiedCount = 3,
                UntrackedCount = 0,
                StashCount = 2,
                RemoteUrl = "https://github.com/Behrad87/DevPurge",
                LastCommitHash = "a3c8941",
                LastCommitAuthor = "Behrad",
                LastCommitRelativeTime = "2 hours ago",
                LastCommitMessage = "refactor: optimize recursive scanning speed"
            },
            new()
            {
                Name = "GeneralBankPaymentEngine.Clean",
                Path = @"D:\repos\GeneralBankPaymentEngine.Clean",
                CurrentBranch = "develop",
                UpstreamBranch = "origin/develop",
                AheadCount = 0,
                ModifiedCount = 2,
                UntrackedCount = 0,
                StashCount = 0,
                RemoteUrl = "https://github.com/Behrad87/GeneralBankPaymentEngine.Clean",
                LastCommitHash = "c91e023",
                LastCommitAuthor = "Behrad",
                LastCommitRelativeTime = "Yesterday",
                LastCommitMessage = "fix(settlement): validate idempotency tokens"
            },
            new()
            {
                Name = "Dia.GandomMonitoring.App",
                Path = @"D:\repos\Dia.GandomMonitoring.App",
                CurrentBranch = "bugfix/telemetry-reconnect",
                UpstreamBranch = "origin/bugfix/telemetry-reconnect",
                AheadCount = 0,
                ModifiedCount = 0,
                UntrackedCount = 0,
                StashCount = 1,
                RemoteUrl = "https://github.com/Behrad87/Dia.GandomMonitoring.App",
                LastCommitHash = "e82d109",
                LastCommitAuthor = "Behrad",
                LastCommitRelativeTime = "3 days ago",
                LastCommitMessage = "chore: update websocket reconnect backoff"
            }
        };

        foreach (var repo in mockRepos)
        {
            var item = new RepoItemViewModel(repo, launcherService, scannerService, gitRunner, _ => { });
            vm.AllRepositories.Add(item);
            vm.DisplayRepositories.Add(item);
        }

        var window = new MainWindow(vm);
        if (window.Content is FrameworkElement mainElement)
        {
            mainElement.Width = 1120;
            mainElement.Height = 720;
            mainElement.Measure(new System.Windows.Size(1120, 720));
            mainElement.Arrange(new Rect(0, 0, 1120, 720));
            mainElement.UpdateLayout();

            var rtb = new RenderTargetBitmap(1120, 720, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(mainElement);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var outPath = Path.Combine(assetsDir, "reporadar_dashboard.png");
            using (var fs = File.Create(outPath))
            {
                encoder.Save(fs);
            }

            // 2. Capture Settings modal
            var sampleSettings = new WorkspaceSettings
            {
                WorkspaceFolders = new List<string> { @"D:\repos", @"C:\Users\Behrad\Code" },
                ScanIntervalMinutes = 10,
                PreferredEditor = EditorType.VSCode,
                NotifyOnDirtyBeforeEndOfDay = true,
                EndOfDayHour = 18,
                CloseToTray = true
            };
            vm.SettingsVm = new SettingsViewModel(sampleSettings, settingsService, _ => { });
            vm.IsSettingsOpen = true;

            mainElement.UpdateLayout();
            var rtbSettings = new RenderTargetBitmap(1120, 720, 96, 96, PixelFormats.Pbgra32);
            rtbSettings.Render(mainElement);

            var encoderSettings = new PngBitmapEncoder();
            encoderSettings.Frames.Add(BitmapFrame.Create(rtbSettings));
            var settingsOutPath = Path.Combine(assetsDir, "reporadar_settings.png");
            using (var fs = File.Create(settingsOutPath))
            {
                encoderSettings.Save(fs);
            }
        }
    }
}
