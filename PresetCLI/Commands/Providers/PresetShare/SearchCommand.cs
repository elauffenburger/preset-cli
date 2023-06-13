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

    private class UI
    {
        private Window _window;
        private Label? _loadingLabel;

        public UI()
        {
            _window = new Window
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            var menu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => {
                        Application.RequestStop ();
                    })
                }),
            });

            Application.Top.Add(_window, menu);
        }

        public void OnLoadStart()
        {
            _loadingLabel = new Label { Text = "Loading..." };
            _window!.Add(_loadingLabel);
        }

        public void OnLoadEnd(List<SearchResult> results)
        {
            _window!.Remove(_loadingLabel);
            _window!.Add(CreateResultsListView(results));
        }

        private ListView CreateResultsListView(List<SearchResult> results)
        {
            var _itemWidth = 50;
            var _itemHeight = 1;

            var scrollViewWidth = _window!.Bounds.Width / 3;
            var scrollViewHeight = _window!.Bounds.Height;

            var list = new ListView(results.Select(result => result.Name).ToList())
            {
                Width = 50,
                Height = scrollViewHeight,
                ColorScheme = Colors.TopLevel,
            };

            for (var i = 0; i < results.Count(); i++)
            {
                var result = results[i];

                list.Add(new TextField
                {
                    ReadOnly = true,
                    Text = result.Name,
                    Y = i * _itemHeight,
                    Width = _itemWidth,
                    ColorScheme = Colors.Dialog,
                });
            }

            return list;
        }
    }

    public override async ValueTask ExecuteAsync(IConsole console)
    {
        var ui = new UI();

        await base.ExecuteAsync(console);

        // Start loading...
        ui.OnLoadStart();

        var req = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = BuildRequestURI()
        };

        req.Headers.Add("cookie", $"PHPSESSID={_config.Providers.PresetShare.SessionID}");

        var res = await _client.SendAsync(req);
        if (res.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new CommandException("");
        }

        var results = ParseResults(await res.Content.ReadAsStringAsync()).ToList();

        // Stop loading and display results.
        ui.OnLoadEnd(results);
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

                return new SearchResult(
                    ID: id ?? 0,
                    Name: node.QuerySelector(".preset-item__name")?.InnerText?.Trim() ?? "",
                    Author: downloadButton?.GetAttributeValue("data-author-name", null) ?? "",
                    Description: HtmlToText(node.QuerySelector(".preset-item-info-handle2")?.GetAttributeValue("data-pt-title", null)) ?? "",
                    PreviewURL: node.QuerySelector(".presetshare-player")?.GetAttributeValue("data-source", null),
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
