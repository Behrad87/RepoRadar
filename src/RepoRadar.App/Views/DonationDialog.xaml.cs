using System;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace RepoRadar.App.Views;

public partial class DonationDialog : FluentWindow
{
    public DonationDialog()
    {
        InitializeComponent();
    }

    private void OnKofiClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://ko-fi.com/behrad87");
    }

    private void OnReymitClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://reymit.ir/behrad87");
    }

    private void OnSponsorClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/sponsors/Behrad87");
    }

    private void OnStarClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/Behrad87/RepoRadar");
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore
        }
    }
}
