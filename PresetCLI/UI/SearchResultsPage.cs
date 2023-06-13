using PresetCLI.Commands.Providers.PresetShare;
using PresetCLI.Enums;
using Terminal.Gui;

namespace PresetCLI.UI;

public class SearchResultsPage
{
    private readonly IProviderService _providerService;
    private readonly Dictionary<SynthType, ISynthService> _synthServices;
    private readonly NetCoreAudio.Player _player = new();

    private readonly Window _window;

    private bool _loading = false;
    private readonly View _loadingDialog = new Dialog { Text = "Loading..." };

    private List<SearchResult>? _results;
    private ListView? _resultsList;
    private int? _selectedResultIndex;

    private readonly Label _debug;

    public SearchResultsPage(IProviderService providerService, Dictionary<SynthType, ISynthService> synthServices)
    {
        _providerService = providerService;
        _synthServices = synthServices;

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

            detailsPanelText!.Text = GetDetailsText(results[args.Item]);
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
                    var file = await _providerService.DownloadPresetAsync(selected);
                    await _synthServices[selected.Synth].ImportPresetAsync(selected, file);
                    break;
            }
        };
    }

    private void AddLoader()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        Application.Top.Add(_loadingDialog);
        _loadingDialog.SetFocus();
        _window.Enabled = false;
    }

    private void RemoveLoader()
    {
        if (!_loading)
        {
            return;
        }

        _loading = false;
        Application.Top.Remove(_loadingDialog);
        _window.Enabled = true;
    }

    private async Task StartPreviewingAsync(int resultIndex)
    {
        var result = _results![resultIndex];
        if (result.PreviewURL == null)
        {
            return;
        }

        var localPreviewFile = await _providerService.DownloadPreviewAsync(result);
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
            Text = GetDetailsText(result),
            Height = resultsList.Height,
            Width = resultsList.Width,
            WordWrap = true,
        };

        panel.Add(text);
        return true;
    }

    private static string GetDetailsText(SearchResult result) 
        => $"{result.Author}\n-----\n{result.Description}";
}