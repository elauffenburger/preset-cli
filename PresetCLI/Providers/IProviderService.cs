namespace PresetCLI.Providers;

public interface IProviderService
{
    string ProviderName { get; }

    Task<bool> IsDownloaded(PresetSearchResult result);

    Task<string> DownloadPreviewAsync(PresetSearchResult result);
    Task DownloadPresetAsync(PresetSearchResult result);

    Task ClearCacheAsync();
}