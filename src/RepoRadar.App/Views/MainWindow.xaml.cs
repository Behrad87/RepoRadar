using System;
using System.ComponentModel;
using System.Windows;
using RepoRadar.App.ViewModels;
using Wpf.Ui.Controls;

namespace RepoRadar.App.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private bool _isExplicitExit;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();

        Loaded += async (s, e) =>
        {
            await _viewModel.InitializeAsync();
        };
    }

    public void RequestExit()
    {
        _isExplicitExit = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit && _viewModel.Settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Focus();
    }
}
