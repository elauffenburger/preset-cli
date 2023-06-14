using System.Text.RegularExpressions;
using PresetCLI.Enums;
using System.Text;
using System.Web;
using CliFx.Exceptions;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;

namespace PresetCLI.Commands.Providers.PresetShare;

public record SearchOptions(string? Keywords, SynthType Synth, GenreType Genre, SoundType Sound, SortType Sort, int Page) { }

public record SearchResults(List<SearchResult> Results, int Page, int NumPages);

public class PresetShareSearchService
{
    private static readonly Regex _htmlBreakRegex = new("<br ?/>");

    private readonly Config _config;
    private readonly Func<HttpClient> _clientFn;
    private readonly SynthTypeConverter _synthTypeConverter;

    public PresetShareSearchService(Config config, Func<HttpClient> clientFn, SynthTypeConverter synthTypeConverter)
    {
        _config = config;
        _clientFn = clientFn;
        _synthTypeConverter = synthTypeConverter;
    }

    public async Task<SearchResults> SearchAsync(SearchOptions search)
    {
        var client = _clientFn();

        var res = await client.GetAsync(BuildRequestURI(search));
        if (res.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new CommandException("");
        }

        return ParseResults(await res.Content.ReadAsStringAsync());
    }

    private Uri BuildRequestURI(SearchOptions search)
    {
        var query = new StringBuilder();
        query.Append($"query={HttpUtility.UrlEncode(search.Keywords)}");
        query.Append($"&instrument={ToQueryValue(search.Synth)}");
        query.Append($"&genre={ToQueryValue(search.Genre)}");
        query.Append($"&type={ToQueryValue(search.Sound)}");
        query.Append($"&orderby={ToQueryValue(search.Sort)}");
        query.Append($"&page={search.Page}");

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

    private SearchResults ParseResults(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var (page, numPages) = doc.DocumentNode
            .QuerySelectorAll("ul.paginator > li")
            .Aggregate((0, 0), (acc, node) =>
            {
                // If this li has an .active class, this is the current page.
                if (node.HasClass("active"))
                {
                    return (acc.Item1 + 1, acc.Item2 + 1);
                }

                return node.InnerText switch
                {
                    "<<" or ">>" => acc,
                    _ => (acc.Item1, acc.Item2 + 1)
                };
            });


        var results = doc.DocumentNode
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
                    Synth: _synthTypeConverter.Convert(node.QuerySelector(".preset-item__info > .link-success").InnerText.ToLower()),
                    Name: node.QuerySelector(".preset-item__name")?.InnerText?.Trim() ?? "",
                    Author: downloadButton?.GetAttributeValue("data-author-name", null) ?? "",
                    Description: HtmlToText(node.QuerySelector(".preset-item-info-handle2")?.GetAttributeValue("data-pt-title", null)) ?? "",
                    PreviewURL: previewURL == null ? null : _config.Providers.PresetShare.BaseURI + previewURL,
                    DownloadURL: id == null ? "" : $"{_config.Providers.PresetShare.BaseURI}/download/index?id={id}"
                );
            })
            .Where(result => !result.IsPremium)
            .ToList();

        return new SearchResults(Results: results, Page: page, NumPages: numPages);
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
