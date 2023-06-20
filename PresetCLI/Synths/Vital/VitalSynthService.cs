using PresetCLI.Configuration;

namespace PresetCLI.Synths.Vital;

public class VitalSynthService : SynthService
{
    private readonly Config _config;

    public VitalSynthService(Config config)
    {
        _config = config;
    }

    public override string PresetPath(PresetSearchResult result)
    {
        return Path.Join(_config.Synths.Vital.PresetsDir, result.Author, "Presets", $"{NormalizeFileName(result.Name)}.vital");
    }
}