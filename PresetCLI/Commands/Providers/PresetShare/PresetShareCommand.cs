using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;

namespace PresetCLI.Commands.Providers.PresetShare;

public abstract class PresetShareCommand : ICommand
{
    protected Config _config;

    [CommandOption("session")]
    public string? SessionID { get; init; }

    [CommandOption("identity")]
    public string? Identity { get; init; }

    protected PresetShareCommand(Config config)
    {
        _config = config;
    }

    public virtual ValueTask ExecuteAsync(IConsole console)
    {
        if (!string.IsNullOrEmpty(SessionID))
        {
            _config.Providers.PresetShare.SessionID = SessionID;
        }

        if(!string.IsNullOrEmpty(Identity)) {
            _config.Providers.PresetShare.Identity = Identity;
        }

        if (string.IsNullOrEmpty(_config.Providers.PresetShare.SessionID))
        {
            throw new CommandException("");
        }

        return ValueTask.CompletedTask;
    }
}