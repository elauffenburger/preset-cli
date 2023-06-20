using System.Diagnostics;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace PresetCLI.Commands;

public abstract class BaseCommand : ICommand
{
    [CommandOption("debug")]
    public bool Debug { get; init; } = false;

    public async virtual ValueTask ExecuteAsync(IConsole console)
    {
        if (Debug)
        {
            while (!Debugger.IsAttached)
            {
                console.Output.WriteLine("waiting for debugger...");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}