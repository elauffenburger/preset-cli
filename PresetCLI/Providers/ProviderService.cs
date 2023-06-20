using PresetCLI.Enums;
using PresetCLI.Synths;

namespace PresetCLI.Providers;

public abstract class ProviderService : IProviderService
{
    protected readonly Func<HttpClient> _clientFn;
    protected readonly Dictionary<SynthType, ISynthService> _synthServices;

    public ProviderService(Func<HttpClient> clientFn, Dictionary<SynthType, ISynthService> synthServices)
    {
        _clientFn = clientFn;
        _synthServices = synthServices;
    }

    public abstract string ProviderName { get; }

    public async Task<bool> IsDownloaded(PresetSearchResult result)
    {
        return File.Exists(_synthServices[result.Synth].PresetPath(result));
    }

    public async Task<string> DownloadPreviewAsync(PresetSearchResult result)
    {
        var path = Path.Join(CreateCacheDir("previews"), result.ID.ToString());
        return await DownloadURLTo(result.PreviewURL!, path);
    }

    public Task DownloadPresetAsync(PresetSearchResult result)
    {
        var path = _synthServices[result.Synth].PresetPath(result);
        return DownloadURLTo(result.DownloadURL, path);
    }

    public async Task ClearCacheAsync()
    {
        if (!Directory.Exists(RootProviderCacheDir))
        {
            return;
        }

        Directory.Delete(RootProviderCacheDir, true);
    }

    private string CreateCacheDir(string dir) => Directory.CreateDirectory($"{RootProviderCacheDir}/{dir}").FullName;

    private async Task<string> DownloadURLTo(string url, string path)
    {
        if (!File.Exists(path))
        {
            // Create the directory for the preset.
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var client = _clientFn();

            var res = await client.GetAsync(url);
            await File.WriteAllBytesAsync(path, await res.Content.ReadAsByteArrayAsync());
        }

        return path;
    }

    private string RootProviderCacheDir => $"{Path.GetTempPath()}/preset-cli/{ProviderName}";
}