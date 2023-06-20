using CliFx.Exceptions;
using CliFx.Extensibility;
using PresetCLI.Enums;

namespace PresetCLI.Providers.PresetShare;

public enum SoundType
{
    Any,
    Arp,
    Atmosphere,
    Bass,
    Chord,
    Drone,
    Drums,
    FX,
    Keys,
    Lead,
    Misc,
    Pad,
    Pluck,
    Reese,
    Seq,
    Stab,
    Sub,
    Synth,
    Vox,
}

public class SoundTypeConverter : BindingConverter<SoundType>
{
    public override SoundType Convert(string? rawValue) => rawValue switch
    {
        "arp" => SoundType.Arp,
        "atmosphere" => SoundType.Atmosphere,
        "bass" => SoundType.Bass,
        "chord" => SoundType.Chord,
        "drone" => SoundType.Drone,
        "drums" => SoundType.Drums,
        "fx" => SoundType.FX,
        "keys" => SoundType.Keys,
        "lead" => SoundType.Lead,
        "misc" => SoundType.Misc,
        "pad" => SoundType.Pad,
        "pluck" => SoundType.Pluck,
        "reese" => SoundType.Reese,
        "seq" => SoundType.Seq,
        "stab" => SoundType.Stab,
        "sub" => SoundType.Sub,
        "synth" => SoundType.Synth,
        "vox" => SoundType.Vox,
        null => SoundType.Any,
        _ => throw new CommandException("")
    };
}

public enum GenreType
{
    Any,
    House,
    Synthwave,
    DnB,
}

public class GenreTypeConverter : BindingConverter<GenreType>
{
    public override GenreType Convert(string? rawValue) => rawValue switch
    {
        "house" => GenreType.House,
        "synthwave" => GenreType.Synthwave,
        "dnb" => GenreType.DnB,
        null => GenreType.Any,
        _ => throw new CommandException("")
    };
}

public class SynthTypeConverter : BindingConverter<SynthType>
{
    public override SynthType Convert(string? rawValue) => rawValue switch
    {
        "vital" => SynthType.Vital,
        "serum" => SynthType.Serum,
        null => SynthType.Any,
        _ => throw new CommandException(""),
    };
}

public enum SortType
{
    Relevance,
    Earliest,
    MostLiked,
    MostDownloaded,
    MostCommented,
    Random,
}

public class SortTypeConverter : BindingConverter<SortType>
{
    public override SortType Convert(string? rawValue) => rawValue switch
    {
        "relevance" => SortType.Relevance,
        "earliest" => SortType.Earliest,
        "likes" => SortType.MostLiked,
        "comments" => SortType.MostCommented,
        "random" => SortType.Random,
        null => SortType.Relevance,
        _ => throw new CommandException(""),
    };
}