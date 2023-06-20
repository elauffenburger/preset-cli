using System.Text.RegularExpressions;
using PresetCLI.Enums;
using System.Text;
using System.Web;
using CliFx.Exceptions;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Net;
using PresetCLI.Configuration;

namespace PresetCLI.Providers.PresetShare;

public record SearchOptions(string? Keywords, SynthType Synth, GenreType Genre, SoundType Sound, SortType Sort, int Page) { }

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

    public async Task<PresetSearchResults> SearchAsync(SearchOptions search)
    {
        var client = _clientFn();

        var res = await client.GetAsync(BuildRequestURI(search));
        return res.StatusCode switch
        {
            HttpStatusCode.OK => ParseResults(await res.Content.ReadAsStringAsync()),
            HttpStatusCode.NotFound => new PresetSearchResults(new(), 1, 1),
            _ => throw new CommandException(""),
        };
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
        SoundType.Arp => "1",
        SoundType.Atmosphere => "14",
        SoundType.Bass => "7",
        SoundType.Chord => "10",
        SoundType.Drone => "13",
        SoundType.Drums => "3",
        SoundType.FX => "4",
        SoundType.Keys => "6",
        SoundType.Lead => "11",
        SoundType.Misc => "18",
        SoundType.Pad => "12",
        SoundType.Pluck => "5",
        SoundType.Reese => "8",
        SoundType.Seq => "2",
        SoundType.Stab => "9",
        SoundType.Sub => "17",
        SoundType.Synth => "16",
        SoundType.Vox => "15",
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

    private PresetSearchResults ParseResults(string html)
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

                return new PresetSearchResult(
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

        return new PresetSearchResults(Results: results, Page: page, NumPages: numPages);
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
