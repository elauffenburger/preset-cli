using System.Runtime.CompilerServices;
using System.Web;
using CliFx.Exceptions;
using Terminal.Gui;

namespace PresetCLI.UI;

public class SearchResultsPage
{
    private readonly HttpClient _client;
    private readonly NetCoreAudio.Player _player = new();

    private readonly Window _window;
    private readonly View _loadingDialog = new Dialog { Text = "Loading..." };

    private List<SearchResult>? _results;
    private ListView? _resultsList;
    private int? _selectedResultIndex;

    private readonly Label _debug;

    public SearchResultsPage(HttpClient client)
    {
        _client = client;

        _window = new Window
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _window.Unloaded += () => _player.Stop();

        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_Quit", "", () => Application.RequestStop())
            }),
        });

        _debug = new Label
        {
            X = 0,
            Y = Pos.Bottom(_window),
            Width = _window.Bounds.Width,
            Height = 1,
            CanFocus = false,
            ColorScheme = Colors.TopLevel,
        };

        Application.Top.Add(_window, menu, _debug);
    }

    public void OnLoadResultsStart()
    {
        _window.Add(_loadingDialog);
    }

    public void OnLoadResultsEnd(List<SearchResult> results)
    {
        _results = results;

        // Stop loading.
        _window.Remove(_loadingDialog);

        // If there were no results, bail.
        if (results.Count() == 0)
        {
            return;
        }

        // Add a results list view.
        _resultsList = CreateResultsListView(results);
        _window.Add(_resultsList);

        // Track that we're currently on the first item.
        _selectedResultIndex = 0;

        // Add a details panel for the selected result.
        if (CreateResultDetailsPanel(_resultsList, results[_selectedResultIndex.Value], out var detailsPanel, out var detailsPanelText))
        {
            _window.Add(detailsPanel);
        }

        // Listen for selection changes to the results list.
        _resultsList.SelectedItemChanged += args =>
        {
            _selectedResultIndex = args.Item;

            detailsPanelText!.Text = results[args.Item].Description;
            detailsPanelText.SetNeedsDisplay();
        };

        // Listen for attempts to preview a sample.
        _resultsList.KeyUp += async args =>
        {
            switch (args.KeyEvent.Key)
            {
                case Key.Space:
                    await StartPreviewingAsync(_selectedResultIndex.Value);
                    break;

                case Key.Enter:
                    var selected = _results[_selectedResultIndex.Value];
                    var file = await DownloadPresetAsync(selected.DownloadURL);
                    await LoadPresetIntoSynthAsync(selected, file);
                    break;
            }
        };
    }

    private void AddLoader()
    {
        Application.Top.Add(_loadingDialog);
        _loadingDialog.SetFocus();
        _window.Enabled = false;
    }

    private void RemoveLoader()
    {
        Application.Top.Remove(_loadingDialog);
        _window.Enabled = true;
    }

    private async Task StartPreviewingAsync(int resultIndex)
    {
        var previewURL = _results![resultIndex].PreviewURL;
        if (previewURL == null)
        {
            return;
        }

        var localPreviewFile = await DownloadPreview(previewURL);
        await _player.Stop();
        await _player.Play(localPreviewFile);

        _resultsList!.Subviews.ElementAt(resultIndex).Border.BorderStyle = BorderStyle.Single;
    }

    private ListView CreateResultsListView(List<SearchResult> results)
    {
        var list = new ListView(results.Select(result => result.Name).ToList())
        {
            Width = 50,
            Height = _window.Bounds.Height,
            ColorScheme = Colors.TopLevel,
        };

        for (var i = 0; i < results.Count(); i++)
        {
            var result = results[i];

            list.Add(new TextView
            {
                ReadOnly = true,
                Text = result.Name,
                ColorScheme = Colors.Dialog,
                CanFocus = false,
                Border = new Border
                {
                    BorderStyle = BorderStyle.None,
                    BorderBrush = Color.Red,
                    BorderThickness = new Thickness(1),
                },
            });
        }

        return list;
    }

    private bool CreateResultDetailsPanel(View resultsList, SearchResult result, out PanelView? panel, out TextView? text)
    {
        if (resultsList == null)
        {
            panel = null;
            text = null;
            return false;
        }

        panel = new PanelView
        {
            LayoutStyle = LayoutStyle.Computed,
            X = Pos.Right(resultsList) + 1,
        };

        text = new TextView
        {
            ReadOnly = true,
            Text = $"{result.Author}\n-----\n{result.Description}",
            Height = resultsList.Height,
            Width = resultsList.Width,
            WordWrap = true,
        };

        panel.Add(text);
        return true;
    }

    private async Task<string> DownloadPreview(string url)
    {
        var path = Path.Join(CreateCacheDir("previews"), HttpUtility.UrlEncode(url));

        return await DownloadURLTo(url, path);
    }

    private Task<string> DownloadPresetAsync(string url)
    {
        var path = Path.Join(CreateCacheDir("presets"), HttpUtility.UrlEncode(url));

        return DownloadURLTo(url, path);
    }

    private string CreateCacheDir(string dir) => Directory.CreateDirectory($"{Path.GetTempPath()}/preset-cli/{dir}/").FullName;

    private async Task<string> DownloadURLTo(string url, string path)
    {
        // If we haven't already downloaded this file, 
        if (!File.Exists(path))
        {
            AddLoader();
            var res = await _client.GetAsync(url);
            await File.WriteAllBytesAsync(path, await res.Content.ReadAsByteArrayAsync());
            RemoveLoader();
        }

        return path;
    }

    private async Task LoadPresetIntoSynthAsync(SearchResult result, string filePath)
    {
        switch (result.Provider)
        {
            case ProviderType.PresetShare:
                break;

            default:
                throw new CommandException("");
        }
    }
}