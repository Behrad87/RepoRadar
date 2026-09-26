using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;

namespace RepoRadar.App.Services;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ILauncherService _launcherService;
    private readonly Action _onShowDashboard;
    private readonly Action _onScanRequested;
    private readonly Action _onShowSettings;
    private readonly Action _onShowDonation;
    private readonly Action _onExitRequested;

    private List<GitRepositoryInfo> _currentDirtyRepos = new();
    private EditorType _preferredEditor = EditorType.VSCode;

    public TrayIconManager(
        ILauncherService launcherService,
        Action onShowDashboard,
        Action onScanRequested,
        Action onShowSettings,
        Action onShowDonation,
        Action onExitRequested)
    {
        _launcherService = launcherService;
        _onShowDashboard = onShowDashboard;
        _onScanRequested = onScanRequested;
        _onShowSettings = onShowSettings;
        _onShowDonation = onShowDonation;
        _onExitRequested = onExitRequested;

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "RepoRadar - Git Workspace Monitor"
        };

        // Load icon reliably from WPF Pack URI or disk
        try
        {
            var resStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"));
            if (resStream != null)
            {
                using var stream = resStream.Stream;
                _notifyIcon.Icon = new Icon(stream);
            }
            else
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                _notifyIcon.Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
            }
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }

        _notifyIcon.Click += (s, e) =>
        {
            if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
            {
                _onShowDashboard();
            }
        };

        _notifyIcon.DoubleClick += (s, e) => _onShowDashboard();

        RebuildContextMenu();
    }

    public void UpdateStatus(IReadOnlyList<GitRepositoryInfo> allRepos, EditorType preferredEditor)
    {
        _preferredEditor = preferredEditor;
        _currentDirtyRepos = allRepos.Where(r => r.IsDirty).ToList();

        var dirtyCount = _currentDirtyRepos.Count;
        var totalCount = allRepos.Count;

        if (dirtyCount > 0)
        {
            var tooltip = $"RepoRadar: {dirtyCount} of {totalCount} repos need attention";
            _notifyIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
        }
        else
        {
            var tooltip = $"RepoRadar: All {totalCount} repos clean & synced";
            _notifyIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
        }

        RebuildContextMenu();
    }

    public void ShowEndOfDayAlert(int dirtyCount, int unpushedCount, int uncommittedCount)
    {
        _notifyIcon.ShowBalloonTip(
            5000,
            "RepoRadar: End of Day Git Alert",
            $"You have {dirtyCount} repositories with unfinished work ({unpushedCount} unpushed commits, {uncommittedCount} modified/uncommitted files). Don't forget to commit or push before shutting down!",
            ToolTipIcon.Warning);
    }

    private void RebuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        // Header
        var dirtyCount = _currentDirtyRepos.Count;
        var headerText = dirtyCount > 0
            ? $"⚠️ {dirtyCount} Repositories Require Attention"
            : "✓ All Repositories Clean";

        var headerItem = new ToolStripMenuItem(headerText)
        {
            Enabled = false,
            Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold)
        };
        menu.Items.Add(headerItem);

        // List top dirty repositories for quick 1-click launch
        if (_currentDirtyRepos.Count > 0)
        {
            menu.Items.Add(new ToolStripSeparator());

            foreach (var repo in _currentDirtyRepos.Take(7))
            {
                var repoSummary = new List<string>();
                if (repo.AheadCount > 0) repoSummary.Add($"↑{repo.AheadCount}");
                if (repo.ModifiedCount > 0) repoSummary.Add($"●{repo.ModifiedCount}");
                if (repo.StashCount > 0) repoSummary.Add($"📦{repo.StashCount}");

                var summaryStr = string.Join(" ", repoSummary);
                var repoItemText = string.IsNullOrEmpty(summaryStr)
                    ? repo.Name
                    : $"{repo.Name} ({summaryStr})";

                var repoItem = new ToolStripMenuItem(repoItemText);
                var repoPath = repo.Path;
                repoItem.Click += (s, e) => _launcherService.OpenInEditor(repoPath, _preferredEditor);
                menu.Items.Add(repoItem);
            }

            if (_currentDirtyRepos.Count > 7)
            {
                var moreItem = new ToolStripMenuItem($"...and {_currentDirtyRepos.Count - 7} more (open dashboard)")
                {
                    Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Italic)
                };
                moreItem.Click += (s, e) => _onShowDashboard();
                menu.Items.Add(moreItem);
            }
        }

        menu.Items.Add(new ToolStripSeparator());

        // Actions
        var scanItem = new ToolStripMenuItem("🔄 Scan Now", null, (s, e) => _onScanRequested());
        menu.Items.Add(scanItem);

        var dashItem = new ToolStripMenuItem("🖥️ Open Dashboard", null, (s, e) => _onShowDashboard())
        {
            Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold)
        };
        menu.Items.Add(dashItem);

        var settingsItem = new ToolStripMenuItem("⚙️ Settings", null, (s, e) => _onShowSettings());
        menu.Items.Add(settingsItem);

        var donateItem = new ToolStripMenuItem("💖 Support & Donate", null, (s, e) => _onShowDonation());
        menu.Items.Add(donateItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("❌ Exit RepoRadar", null, (s, e) => _onExitRequested());
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
