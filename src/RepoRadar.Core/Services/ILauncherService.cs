using System.Threading.Tasks;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public interface ILauncherService
{
    bool OpenInEditor(string repoPath, EditorType editorType);
    bool OpenInTerminal(string repoPath);
    bool OpenInExplorer(string repoPath);
    bool OpenInBrowser(string url);
}
