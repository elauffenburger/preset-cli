﻿using System.Net;

using CliFx;
using CliFx.Extensibility;

using Microsoft.Extensions.DependencyInjection;

using PresetCLI.Configuration;
using PresetCLI.Enums;
using PresetCLI.Providers;
using PresetCLI.Providers.PresetShare;
using PresetCLI.Synths;
using PresetCLI.Synths.Vital;

namespace PresetCLI;

public record PresetSearchResult(int ID, ProviderType Provider, bool IsPremium, SynthType Synth, string Name, string Author, string Description, string? PreviewURL, string DownloadURL) { }

public record PresetSearchResults(List<PresetSearchResult> Results, int Page, int NumPages) { }

public class Program
{
    public static async Task<int> Main()
    {
        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .UseTypeActivator(cmds =>
            {
                var services = new ServiceCollection();

                // Register config.
                services.AddSingleton(services =>
                {
                    var homeDir = Environment.GetEnvironmentVariable("HOME");

                    var configFilePath = Path.Join(homeDir, ".presetclirc");
                    var configFileVars = File.Exists(configFilePath)
                        ? File.ReadAllLines(configFilePath)
                            .Aggregate(new Dictionary<string, string>(), (acc, line) =>
                            {
                                var lineParts = line.Split("=", 2);
                                acc[lineParts[0].ToLowerInvariant()] = lineParts[1];

                                return acc;
                            })
                        : new Dictionary<string, string>();

                    string? GetConfigVar(string name)
                    {
                        name = name.ToLowerInvariant();
                        return configFileVars!.TryGetValue(name, out var value) ? value : Environment.GetEnvironmentVariable($"PRESET_CLI_{name}");
                    }

                    return new Config
                    {
                        Providers = new Config.ProvidersConfig
                        {
                            PresetShare = new Config.ProvidersConfig.PresetShareConfig
                            {
                                BaseURI = "https://presetshare.com",
                                SessionID = GetConfigVar("PRESETSHARE_SESSIONID"),
                                Identity = GetConfigVar("PRESETSHARE_IDENTITY"),
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
                        handler.CookieContainer.Add(new Uri("https://presetshare.com"), new Cookie("_identity", config.Providers.PresetShare.Identity));

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