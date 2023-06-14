using System.Diagnostics;
using PresetCLI.Enums;
using Terminal.Gui;

namespace PresetCLI.UI;

public class SearchResultsPage
{
    private readonly IProviderService _providerService;
    private readonly Dictionary<SynthType, ISynthService> _synthServices;
    private readonly List<SearchResult> _results;
    private readonly NetCoreAudio.Player _player = new();

    private Window? _window;
    private Dialog? _loadingDialog;
    private ListView? _resultsList;
    private int? _selectedResultIndex;

    public SearchResultsPage(IProviderService providerService, Dictionary<SynthType, ISynthService> synthServices, List<SearchResult> results)
    {
        _providerService = providerService;
        _synthServices = synthServices;
        _results = results;
    }

    public void Start()
    {
        // If there were no results, bail.
        if (_results.Count() == 0)
        {
            return;
        }

        Application.Init();

        _window = new Window
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _window.Unloaded += () => _player.Stop();

        _loadingDialog = new Dialog
        {
            Text = "Loading...",
        };

        // Add a results list view.
        _resultsList = CreateResultsListView(_results);
        _window.Add(_resultsList);

        // Track that we're currently on the first item.
        _selectedResultIndex = 0;

        // Add a details panel for the selected result.
        if (CreateResultDetailsPanel(_resultsList, _results[_selectedResultIndex.Value], out var detailsPanel, out var detailsPanelText))
        {
            _window.Add(detailsPanel);
        }

        // Listen for selection changes to the results list.
        _resultsList.SelectedItemChanged += args =>
        {
            _selectedResultIndex = args.Item;

            detailsPanelText!.Text = GetDetailsText(_results[args.Item]);
            detailsPanelText.SetNeedsDisplay();
        };

        // Listen for attempts to preview a sample.
        _resultsList.KeyPress += async args =>
        {
            switch (args.KeyEvent.Key)
            {
                case Key.Space:
                    AddLoader();
                    await StartPreviewingAsync(_selectedResultIndex.Value);
                    RemoveLoader();

                    break;

                case Key.Enter:
                    AddLoader();
                    var selected = _results[_selectedResultIndex.Value];
                    var file = await _providerService.DownloadPresetAsync(selected);
                    await _synthServices[selected.Synth].ImportPresetAsync(selected, file);
                    RemoveLoader();

                    break;
            }
        };

        Debugger.Launch();

        Application.Run(_window);
        Application.Shutdown();
    }

    private void AddLoader()
    {
        Application.Top.Add(_loadingDialog);
    }

    private void RemoveLoader()
    {
        Application.Top.Remove(_loadingDialog);
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
            Height = Dim.Fill(),
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