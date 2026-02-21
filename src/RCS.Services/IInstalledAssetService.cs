using RCS.Data.Entities;

namespace RCS.Services;

public interface IInstalledAssetService<T> where T : InstalledAsset
{
    Task<List<T>> LoadAsync(string projectId);
    Task<T> UpsertAsync(string projectId, T row);
    Task DeleteAsync(string projectId, string rowId);
}
