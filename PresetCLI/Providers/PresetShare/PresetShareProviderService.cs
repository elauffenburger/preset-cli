using PresetCLI.Enums;

namespace PresetCLI.Providers.PresetShare;

public class PresetShareProviderService : ProviderService
{
    public PresetShareProviderService(Func<HttpClient> clientFn, Dictionary<SynthType, ISynthService> synthServices) : base(clientFn, synthServices) { }

    public override string ProviderName => "preset-share";
}