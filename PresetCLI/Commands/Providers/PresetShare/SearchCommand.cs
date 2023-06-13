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

namespace PresetCLI.Commands.Providers.PresetShare;

[Command("presetshare search")]
public class SearchCommand : PresetShareCommand
{
    private static readonly Regex _htmlBreakRegex = new("<br ?/>");

    private readonly HttpClient _client;

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

    public SearchCommand(Config config, HttpClient client) : base(config)
    {
        _client = client;
    }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        await base.ExecuteAsync(console);
        _client.DefaultRequestHeaders.Add("cookie", $"PHPSESSID={_config.Providers.PresetShare.SessionID}");

        var ui = new SearchResultsPage(_client);

        // Start loading...
        ui.OnLoadResultsStart();

        var res = await _client.GetAsync(BuildRequestURI());
        if (res.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new CommandException("");
        }

        var results = ParseResults(await res.Content.ReadAsStringAsync()).ToList();

        // Stop loading and display results.
        ui.OnLoadResultsEnd(results);
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

        return doc.DocumentNode
            .QuerySelectorAll(".preset-item")
            .Select(node =>
            {
                var downloadButton = node.QuerySelector("[data-author-name][data-preset-id]");
                var id = downloadButton?.GetAttributeValue<int?>("data-preset-id", null);
                var previewURL = node.QuerySelector(".presetshare-player")?.GetAttributeValue("data-source", null);

                return new SearchResult(
                    ID: id ?? 0,
                    Name: node.QuerySelector(".preset-item__name")?.InnerText?.Trim() ?? "",
                    Author: downloadButton?.GetAttributeValue("data-author-name", null) ?? "",
                    Description: HtmlToText(node.QuerySelector(".preset-item-info-handle2")?.GetAttributeValue("data-pt-title", null)) ?? "",
                    PreviewURL: previewURL == null ? null : _config.Providers.PresetShare.BaseURI + previewURL,
                    DownloadURL: id == null ? null : $"{_config.Providers.PresetShare.BaseURI}/download/index?id={id}"
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
