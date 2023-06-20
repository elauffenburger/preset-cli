using System.Diagnostics;
using PresetCLI.Commands.Providers.PresetShare;
using PresetCLI.Enums;
using PresetCLI.Providers;
using PresetCLI.Providers.PresetShare;
using Terminal.Gui;

namespace PresetCLI.UI;

public class PresetSearchResultsPage
{
    private readonly IProviderService _providerService;
    private readonly PresetShareSearchService _presetShareSearchService;
    private readonly NetCoreAudio.Player _player = new();

    private PresetSearchResults? _results;

    private Window? _window;
    private Dialog? _loadingDialog;
    private ListView? _resultsList;
    private int? _selectedResultIndex;

    public PresetSearchResultsPage(IProviderService providerService, PresetShareSearchService presetShareSearchService)
    {
        _providerService = providerService;
        _presetShareSearchService = presetShareSearchService;
    }

    public async Task Start(SearchOptions firstSearch)
    {
        _results = await _presetShareSearchService.SearchAsync(firstSearch);

        // If there were no results, bail.
        if (_results.Results.Count() == 0)
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
        _resultsList = await CreateResultsListView(_results.Results);
        _window.Add(_resultsList);

        // Track that we're currently on the first item.
        _selectedResultIndex = 0;

        // Add a details panel for the selected result.
        if (CreateResultDetailsPanel(_resultsList, _results.Results[_selectedResultIndex.Value], out var detailsPanel, out var detailsPanelText))
        {
            _window.Add(detailsPanel);
        }

        // Listen for selection changes to the results list.
        _resultsList.SelectedItemChanged += args =>
        {
            _selectedResultIndex = args.Item;

            detailsPanelText!.Text = GetDetailsText(_results.Results[args.Item]);
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

                case Key.d:
                    AddLoader();

                    var selected = _results!.Results[_selectedResultIndex.Value];
                    await _providerService.DownloadPresetAsync(selected);

                    var selectedItemIndex = _resultsList.SelectedItem;
                    _resultsList.SetSource(await CreateResultsListViewSource(_results.Results));
                    _resultsList.SelectedItem = selectedItemIndex;

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
        var result = _results!.Results[resultIndex];
        if (result.PreviewURL == null)
        {
            return;
        }

        var localPreviewFile = await _providerService.DownloadPreviewAsync(result);
        await _player.Stop();
        await _player.Play(localPreviewFile);
    }

    private async Task<List<string>> CreateResultsListViewSource(List<PresetSearchResult> results)
    {
        var items = new List<string>(results.Count);
        foreach (var result in results)
        {
            var text = await _providerService.IsDownloaded(result) ? $"[✓] {result.Name}" : $"[ ] {result.Name}";
            items.Add(text);
        }

        return items;
    }

    private async Task<ListView> CreateResultsListView(List<PresetSearchResult> results)
    {
        var list = new ListView(await CreateResultsListViewSource(results))
        {
            Width = 50,
            Height = Dim.Fill(),
            ColorScheme = Colors.TopLevel,
        };

        return list;
    }

    private bool CreateResultDetailsPanel(View resultsList, PresetSearchResult result, out PanelView? panel, out TextView? text)
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

    private static string GetDetailsText(PresetSearchResult result)
        => $"{result.Author}\n-----\n{result.Description}";
}