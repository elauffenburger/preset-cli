using System.Diagnostics;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace PresetCLI.Commands;

[Command("cache clear")]
public class ClearCacheCommand : ICommand
{
    private readonly IEnumerable<IProviderService> _providerServices;

    public ClearCacheCommand(IEnumerable<IProviderService> providerServices)
    {
        _providerServices = providerServices;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        foreach (var service in _providerServices)
        {
            await service.ClearCacheAsync();
        }
    }
}