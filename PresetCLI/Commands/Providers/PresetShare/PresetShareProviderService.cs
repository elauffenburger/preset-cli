namespace PresetCLI.Commands.Providers.PresetShare;

public class PresetShareProviderService : ProviderService
{
    public PresetShareProviderService(Func<HttpClient> clientFn) : base(clientFn) { }

    public override string ProviderName => "preset-share";
}