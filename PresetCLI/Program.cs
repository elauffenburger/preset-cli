using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using CliFx;
using CliFx.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using PresetCLI.Commands.Providers.PresetShare;
using PresetCLI.Enums;

namespace PresetCLI;

public class Config
{
    public class ProvidersConfig
    {
        public class PresetShareConfig
        {
            public required string BaseURI;
            public string? SessionID;
            public string? Identity;
        }

        public required PresetShareConfig PresetShare;
    }

    public class SynthsConfig
    {
        public class VitalConfig
        {
            public required string PresetsDir;
        }

        public required VitalConfig Vital;
    }

    public required ProvidersConfig Providers;
    public required SynthsConfig Synths;
}

public enum ProviderType
{
    PresetShare,
}

public interface IProviderService
{
    string ProviderName { get; }

    Task<string> DownloadPreviewAsync(SearchResult result);
    Task<string> DownloadPresetAsync(SearchResult result);

    Task ClearCacheAsync();
}

public abstract class ProviderService : IProviderService
{
    protected readonly Func<HttpClient> _clientFn;

    public ProviderService(Func<HttpClient> clientFn)
    {
        _clientFn = clientFn;
    }

    public abstract string ProviderName { get; }

    public async Task<string> DownloadPreviewAsync(SearchResult result)
    {
        var path = Path.Join(CreateCacheDir("previews"), result.ID.ToString());
        return await DownloadURLTo(result.PreviewURL!, path);
    }

    public Task<string> DownloadPresetAsync(SearchResult result)
    {
        var path = Path.Join(CreateCacheDir("presets"), result.ID.ToString());
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
            var client = _clientFn();

            var res = await client.GetAsync(url);
            await File.WriteAllBytesAsync(path, await res.Content.ReadAsByteArrayAsync());
        }

        return path;
    }

    private string RootProviderCacheDir => $"{Path.GetTempPath()}/preset-cli/{ProviderName}";
}

public interface ISynthService
{
    Task ImportPresetAsync(SearchResult result, string path);
}

public abstract class SynthService : ISynthService
{
    private static readonly Regex _fileNameInvalidCharsRegex = new("[^a-zA-Z0-9_]");

    public abstract Task ImportPresetAsync(SearchResult result, string sourceFile);

    protected string NormalizeFileName(string name)
    {
        return _fileNameInvalidCharsRegex.Replace(name, "_");
    }
}

public class VitalSynthService : SynthService
{
    private readonly Config _config;

    public VitalSynthService(Config config)
    {
        _config = config;
    }

    public override async Task ImportPresetAsync(SearchResult result, string sourceFile)
    {
        var dir = Directory.CreateDirectory(Path.Join(_config.Synths.Vital.PresetsDir, result.Author, "Presets"));
        var target = Path.Join(dir.FullName, $"{NormalizeFileName(result.Name)}.vital");

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        File.Copy(sourceFile, target);
    }
}

public record SearchResult(int ID, ProviderType Provider, bool IsPremium, SynthType Synth, string Name, string Author, string Description, string? PreviewURL, string DownloadURL) { }

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Any(arg => arg == "--debug"))
        {
            while (!Debugger.IsAttached)
            {
                Console.WriteLine("waiting for debugger...");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeActivator(cmds =>
            {
                var services = new ServiceCollection();

                // Register config.
                services.AddSingleton(services =>
                {
                    return new Config
                    {
                        Providers = new Config.ProvidersConfig
                        {
                            PresetShare = new Config.ProvidersConfig.PresetShareConfig
                            {
                                BaseURI = "https://presetshare.com"
                            }
                        },
                        Synths = new Config.SynthsConfig
                        {
                            Vital = new Config.SynthsConfig.VitalConfig
                            {
                                PresetsDir = "/Users/otacon/Music/Vital"
                            }
                        }
                    };
                });

                // Register HttpClient.
                services.AddSingleton<Func<HttpClient>>(services =>
                {
                    return () =>
                    {
                        var config = services.GetRequiredService<Config>();

                        var handler = new HttpClientHandler();
                        handler.CookieContainer.Add(new Uri("https://presetshare.com"), new Cookie("PHPSESSID", config.Providers.PresetShare.SessionID));
                        handler.CookieContainer.Add(new Uri("https://presetshare.com"), new Cookie("_identity", "a03ffadc68a6e52acb1a38f0279938fac7614bbe41aac05b33d7fc54b2d2f2dea%3A2%3A%7Bi%3A0%3Bs%3A9%3A%22_identity%22%3Bi%3A1%3Bs%3A50%3A%22%5B51637%2C%22dLGZjPG_64a2nAOdNP6UpvdKbw1tNiWx%22%2C2592000%5D%22%3B%7D"));

                        return new HttpClient(handler);
                    };
                });

                // Register commands.
                foreach (var cmd in cmds)
                {
                    services.AddTransient(cmd);
                }

                // Register converters.
                foreach (var converter in GetTypesImplementing(typeof(BindingConverter<string>).GetGenericTypeDefinition()))
                {
                    services.AddTransient(converter);
                }

                // Register synth services.
                services.AddSingleton<VitalSynthService>();
                services.AddSingleton(services =>
                {
                    return new Dictionary<SynthType, ISynthService>
                    {
                        {SynthType.Vital, services.GetRequiredService<VitalSynthService>()},
                    };
                });

                // Register provider services.
                services.AddTransient<PresetShareProviderService>();
                services.AddTransient<IProviderService, PresetShareProviderService>();

                // Register misc. services.
                services.AddSingleton<PresetShareSearchService>();

                return services.BuildServiceProvider();
            })
            .Build()
            .RunAsync();
    }

    private static IEnumerable<Type> GetTypesImplementing(Type targetType)
    {
        bool ImplementsType(Type? type)
        {
            if (type == null)
            {
                return false;
            }

            // Check if this _is_ the target type, or it's generic and it matches the targetType, or its base type implements the target type.
            return type.Equals(targetType) ||
                (type.IsGenericType && type.GetGenericTypeDefinition().Equals(targetType)) ||
                ImplementsType(type.BaseType);
        }

        return typeof(Program).Assembly.GetTypes().Where(ImplementsType);
    }
}