namespace PresetCLI.Commands.Providers.PresetShare;

public class PresetShareProviderService : ProviderService
{
    public PresetShareProviderService(HttpClient client) : base(client) { }

    public override string ProviderName => "preset-share";
}