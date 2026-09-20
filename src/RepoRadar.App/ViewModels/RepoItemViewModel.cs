using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RepoRadar.Core.Models;
using RepoRadar.Core.Services;

namespace RepoRadar.App.ViewModels;

public partial class RepoItemViewModel : ObservableObject
{
    private readonly ILauncherService _launcherService;
    private readonly IGitScannerService _scannerService;
    private readonly IGitProcessRunner _gitRunner;
    private readonly Action<RepoItemViewModel> _onRepoUpdated;

    [ObservableProperty]
    private GitRepositoryInfo _model;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string? _actionMessage;

    public RepoItemViewModel(
        GitRepositoryInfo model,
        ILauncherService launcherService,
        IGitScannerService scannerService,
        IGitProcessRunner gitRunner,
        Action<RepoItemViewModel> onRepoUpdated)
    {
        _model = model;
        _launcherService = launcherService;
        _scannerService = scannerService;
        _gitRunner = gitRunner;
        _onRepoUpdated = onRepoUpdated;
    }

    public string Name => Model.Name;
    public string Path => Model.Path;
    public string CurrentBranch => Model.CurrentBranch;
    public string? UpstreamBranch => Model.UpstreamBranch;
    public int AheadCount => Model.AheadCount;
    public int BehindCount => Model.BehindCount;
    public int ModifiedCount => Model.ModifiedCount;
    public int UntrackedCount => Model.UntrackedCount;
    public int StashCount => Model.StashCount;
    public string? RemoteUrl => Model.RemoteUrl;
    public string? LastCommitHash => Model.LastCommitHash;
    public string? LastCommitAuthor => Model.LastCommitAuthor;
    public string? LastCommitMessage => Model.LastCommitMessage;
    public string? LastCommitRelativeTime => Model.LastCommitRelativeTime;
    public string? LastError => Model.LastError;

    public bool HasError => Model.HasError;
    public bool HasUncommitted => Model.HasUncommitted;
    public bool HasUnpushed => Model.HasUnpushed;
    public bool HasUnpulled => Model.HasUnpulled;
    public bool HasStashes => Model.HasStashes;
    public bool IsDirty => Model.IsDirty;
    public bool IsClean => Model.IsClean;
    public bool HasRemote => !string.IsNullOrWhiteSpace(Model.RemoteUrl);

    public string StatusSummary => Model.StatusSummary;

    public string CommitPreview => !string.IsNullOrEmpty(LastCommitMessage)
        ? $"{LastCommitHash} • {LastCommitMessage} ({LastCommitRelativeTime ?? "recently"} by {LastCommitAuthor ?? "you"})"
        : "No commits detected";

    [RelayCommand]
    private void OpenInVSCode()
    {
        _launcherService.OpenInEditor(Path, EditorType.VSCode);
    }

    [RelayCommand]
    private void OpenInRider()
    {
        _launcherService.OpenInEditor(Path, EditorType.Rider);
    }

    [RelayCommand]
    private void OpenInCursor()
    {
        _launcherService.OpenInEditor(Path, EditorType.Cursor);
    }

    [RelayCommand]
    private void OpenInTerminal()
    {
        _launcherService.OpenInTerminal(Path);
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        _launcherService.OpenInExplorer(Path);
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        if (HasRemote && !string.IsNullOrEmpty(RemoteUrl))
        {
            _launcherService.OpenInBrowser(RemoteUrl);
        }
    }

    [RelayCommand]
    private async Task FetchAsync()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        ActionMessage = "Fetching remote...";

        try
        {
            await _gitRunner.RunGitAsync(Path, "fetch --prune");
            var updated = await _scannerService.InspectRepositoryAsync(Path);
            Model = updated;

            OnPropertyChanged(string.Empty);
            _onRepoUpdated(this);
            ActionMessage = "Fetch completed";
        }
        catch (Exception ex)
        {
            ActionMessage = $"Fetch failed: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
            _ = Task.Delay(3000).ContinueWith(_ => ActionMessage = null);
        }
    }
}
