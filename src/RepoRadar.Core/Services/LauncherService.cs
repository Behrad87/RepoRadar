using System;
using System.Diagnostics;
using System.IO;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public class LauncherService : ILauncherService
{
    public bool OpenInEditor(string repoPath, EditorType editorType)
    {
        if (!Directory.Exists(repoPath)) return false;

        return editorType switch
        {
            EditorType.VSCode => TryLaunch("code", $"\"{repoPath}\""),
            EditorType.VSCodeInsiders => TryLaunch("code-insiders", $"\"{repoPath}\""),
            EditorType.Rider => TryLaunch("rider64", $"\"{repoPath}\"") || TryLaunch("rider", $"\"{repoPath}\""),
            EditorType.Cursor => TryLaunch("cursor", $"\"{repoPath}\""),
            EditorType.VisualStudio => TryOpenVisualStudio(repoPath),
            EditorType.Terminal => OpenInTerminal(repoPath),
            EditorType.Explorer => OpenInExplorer(repoPath),
            _ => TryLaunch("code", $"\"{repoPath}\"")
        };
    }

    private static bool TryOpenVisualStudio(string repoPath)
    {
        // Try finding .sln or .slnx file first
        try
        {
            var slnFiles = Directory.GetFiles(repoPath, "*.sln*", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length > 0)
            {
                return TryLaunch(slnFiles[0], string.Empty);
            }
        }
        catch
        {
            // Fallback
        }

        return TryLaunch("devenv", $"\"{repoPath}\"");
    }

    public bool OpenInTerminal(string repoPath)
    {
        if (!Directory.Exists(repoPath)) return false;

        // Try Windows Terminal first
        if (TryLaunch("wt.exe", $"-d \"{repoPath}\""))
        {
            return true;
        }

        // Fallback to PowerShell
        if (TryLaunch("powershell.exe", $"-NoExit -Command \"Set-Location -LiteralPath '{repoPath}'\""))
        {
            return true;
        }

        // Fallback to CMD
        return TryLaunch("cmd.exe", $"/K \"cd /d {repoPath}\"");
    }

    public bool OpenInExplorer(string repoPath)
    {
        if (!Directory.Exists(repoPath)) return false;
        return TryLaunch("explorer.exe", $"\"{repoPath}\"");
    }

    public bool OpenInBrowser(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryLaunch(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
