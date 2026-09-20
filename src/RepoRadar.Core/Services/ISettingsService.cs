using System.Threading.Tasks;
using RepoRadar.Core.Models;

namespace RepoRadar.Core.Services;

public interface ISettingsService
{
    Task<WorkspaceSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(WorkspaceSettings settings);
    string GetSettingsFilePath();
}
