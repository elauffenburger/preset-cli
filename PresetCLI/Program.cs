﻿using CliFx;
using CliFx.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using PresetCLI.Commands.Providers.PresetShare;
using Terminal.Gui;

namespace PresetCLI;

public class Config
{
    public class ProvidersConfig
    {
        public class PresetShareConfig
        {
            public required string BaseURI;
            public string? SessionID;
        }

        public required PresetShareConfig PresetShare;
    }

    public required ProvidersConfig Providers;
}

public enum ProviderType
{
    PresetShare,
}

public record SearchResult(int ID, string Name, string Author, string Description, string? PreviewURL, string? DownloadURL) { }

public class Program
{
    public static async Task<int> Main()
    {
        var result = -1;

        Application.Init();

        try
        {
            Application.MainLoop.Invoke(async () =>
            {
                result = await new CliApplicationBuilder()
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
                                }
                            };
                        });

                        // Register HttpClient.
                        services.AddTransient<HttpClient>();

                        // Register commands.
                        foreach (var cmd in cmds)
                        {
                            services.AddTransient(cmd);
                        }

                        // Add converters.
                        foreach (var converter in GetTypesImplementing(typeof(BindingConverter<string>).GetGenericTypeDefinition()))
                        {
                            services.AddTransient(converter);
                        }

                        return services.BuildServiceProvider();
                    })
                    .Build()
                    .RunAsync();
            });

            Application.Run();

            return result;
        }
        finally
        {
            Application.Shutdown();
        }
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