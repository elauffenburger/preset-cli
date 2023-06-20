namespace PresetCLI.Configuration;

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