using CliFx.Exceptions;
using CliFx.Extensibility;
using PresetCLI.Enums;

namespace PresetCLI.Commands.Providers.PresetShare;

public enum SoundType
{
    Any,
    Bass,
    Pad,
}

public class SoundTypeConverter : BindingConverter<SoundType>
{
    public override SoundType Convert(string? rawValue) => rawValue switch
    {
        "bass" => SoundType.Bass,
        "pad" => SoundType.Pad,
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