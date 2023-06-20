using CliFx.Attributes;
using CliFx.Infrastructure;
using Terminal.Gui;
using PresetCLI.UI;
using PresetCLI.Enums;
using PresetCLI.Providers.PresetShare;
using PresetCLI.Configuration;

namespace PresetCLI.Commands.Providers.PresetShare;

[Command("presetshare search")]
public class SearchCommand : PresetShareCommand
{
    private readonly PresetShareProviderService _presetShareProviderService;
    private readonly PresetShareSearchService _presetShareSearchService;

    [CommandOption("keywords", 'k')]
    public string? Keywords { get; init; }

    [CommandOption("synth", 'y', Converter = typeof(SynthTypeConverter))]
    public SynthType Synth { get; init; } = SynthType.Any;

    [CommandOption("genre", 'g', Converter = typeof(GenreTypeConverter))]
    public GenreType Genre { get; init; } = GenreType.Any;

    [CommandOption("sound", 'u', Converter = typeof(SoundTypeConverter))]
    public SoundType Sound { get; init; } = SoundType.Any;

    [CommandOption("sort", 's', Converter = typeof(SortTypeConverter))]
    public SortType Sort { get; init; } = SortType.Relevance;

    public SearchCommand(Config config, PresetShareSearchService presetShareSearchService, PresetShareProviderService presetShareProviderService) : base(config)
    {
        _presetShareSearchService = presetShareSearchService;
        _presetShareProviderService = presetShareProviderService;
    }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        await base.ExecuteAsync(console);

        var ui = new PresetSearchResultsPage(_presetShareProviderService, _presetShareSearchService);
        await ui.Start(new SearchOptions(Keywords: Keywords, Synth: Synth, Genre: Genre, Sound: Sound, Sort: Sort, Page: 1));
    }
}
