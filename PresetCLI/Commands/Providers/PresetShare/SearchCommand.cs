using System.Text;
using System.Web;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Text.RegularExpressions;
using Terminal.Gui;
using System.Collections;
using PresetCLI.UI;
using PresetCLI.Enums;
using System.ComponentModel;

namespace PresetCLI.Commands.Providers.PresetShare;

[Command("presetshare search")]
public class SearchCommand : PresetShareCommand
{
    private static readonly Regex _htmlBreakRegex = new("<br ?/>");

    private readonly Func<HttpClient> _clientFn;
    private readonly Dictionary<SynthType, ISynthService> _synthServices;
    private readonly PresetShareProviderService _presetShareProviderService;

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

    public SearchCommand(Config config, Func<HttpClient> clientFn, Dictionary<SynthType, ISynthService> synthServices, PresetShareProviderService presetShareProviderService) : base(config)
    {
        _clientFn = clientFn;
        _synthServices = synthServices;
        _presetShareProviderService = presetShareProviderService;
    }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        await base.ExecuteAsync(console);

        var client = _clientFn();

        var res = await client.GetAsync(BuildRequestURI());
        if (res.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new CommandException("");
        }

        var results = ParseResults(await res.Content.ReadAsStringAsync())
            .Where(result => !result.IsPremium)
            .ToList();

        var ui = new SearchResultsPage(_presetShareProviderService, _synthServices, results);
        ui.Start();
    }

    private Uri BuildRequestURI()
    {
        var query = new StringBuilder();
        query.Append($"query={HttpUtility.UrlEncode(Keywords)}");
        query.Append($"&instrument={ToQueryValue(Synth)}");
        query.Append($"&genre={ToQueryValue(Genre)}");
        query.Append($"&type={ToQueryValue(Sound)}");
        query.Append($"&orderby={ToQueryValue(Sort)}");

        return new Uri($"{_config.Providers.PresetShare.BaseURI}/presets?{query}");
    }

    private static string ToQueryValue(SoundType type) => type switch
    {
        SoundType.Any => "",
        SoundType.Bass => "7",
        SoundType.Pad => "12",
        _ => throw new Exception(),
    };

    private static string ToQueryValue(GenreType type) => type switch
    {
        GenreType.Any => "",
        GenreType.House => "4",
        GenreType.Synthwave => "10",
        GenreType.DnB => "1",
        _ => throw new Exception(),
    };

    private static string ToQueryValue(SynthType type) => type switch
    {
        SynthType.Any => "",
        SynthType.Serum => "1",
        SynthType.Vital => "2",
        _ => throw new Exception(),
    };

    private static string ToQueryValue(SortType type) => type switch
    {
        SortType.Relevance => "relevance",
        SortType.Earliest => "created_at",
        SortType.MostLiked => "likes",
        SortType.MostDownloaded => "downloads",
        SortType.MostCommented => "comments",
        SortType.Random => "random",
        _ => throw new Exception(),
    };

    private IEnumerable<SearchResult> ParseResults(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var synthTypeConverter = new SynthTypeConverter();

        return doc.DocumentNode
            .QuerySelectorAll(".preset-item")
            .Select(node =>
            {
                var downloadButton = node.QuerySelector("[data-author-name][data-preset-id]");
                var id = downloadButton?.GetAttributeValue<int?>("data-preset-id", null);
                var previewURL = node.QuerySelector(".presetshare-player")?.GetAttributeValue("data-source", null);

                return new SearchResult(
                    ID: id ?? 0,
                    Provider: ProviderType.PresetShare,
                    IsPremium: downloadButton?.HasClass("for-subs") ?? true,
                    Synth: synthTypeConverter.Convert(node.QuerySelector(".preset-item__info > .link-success").InnerText.ToLower()),
                    Name: node.QuerySelector(".preset-item__name")?.InnerText?.Trim() ?? "",
                    Author: downloadButton?.GetAttributeValue("data-author-name", null) ?? "",
                    Description: HtmlToText(node.QuerySelector(".preset-item-info-handle2")?.GetAttributeValue("data-pt-title", null)) ?? "",
                    PreviewURL: previewURL == null ? null : _config.Providers.PresetShare.BaseURI + previewURL,
                    DownloadURL: id == null ? "" : $"{_config.Providers.PresetShare.BaseURI}/download/index?id={id}"
                );
            })
            .ToList();
    }

    private static string? HtmlToText(string? html)
    {
        if (html == null)
        {
            return null;
        }

        return _htmlBreakRegex.Replace(HttpUtility.HtmlDecode(html), "\n");
    }
}
