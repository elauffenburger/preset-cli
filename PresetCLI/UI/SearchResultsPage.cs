using Terminal.Gui;

namespace PresetCLI.UI;

public class SearchResultsPage
{
    private readonly Window _window;
    private Label? _loadingLabel;

    private List<SearchResult>? _results;
    private ListView? _resultsList;
    private int? _previewedResultIndex;
    private int? _selectedResultIndex;

    public SearchResultsPage()
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
                new MenuItem ("_Quit", "", () => Application.RequestStop())
            }),
        });

        Application.Top.Add(_window, menu);
    }

    public void OnLoadStart()
    {
        _loadingLabel = new Label { Text = "Loading..." };
        _window.Add(_loadingLabel);
    }

    public void OnLoadEnd(List<SearchResult> results)
    {
        // Stop loading.
        _window.Remove(_loadingLabel);

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
        _resultsList.KeyUp += args =>
        {
            if (args.KeyEvent.Key != Key.Enter)
            {
                return;
            }

            // If there isn't a currently previewed result, preview the selected one!
            if (_previewedResultIndex == null)
            {
                StartPreviewing(_selectedResultIndex.Value);
            }
            // Otherwise, if the previewed result _is_ the selected one, stop previewing!
            else if (_results![_previewedResultIndex.Value].ID == _results[_selectedResultIndex.Value].ID)
            {
                StopPreviewing();
            }
            // Otherwise, kill the current preview and preview the newly-requested result!
            else
            {
                StopPreviewing();
                StartPreviewing(_selectedResultIndex.Value);
            }
        };
    }

    private void StartPreviewing(int result)
    {
        _resultsList!.Subviews.ElementAt(result).Border.BorderStyle = BorderStyle.Single;
        _previewedResultIndex = result;
    }

    private void StopPreviewing()
    {
        if (_previewedResultIndex == null)
        {
            return;
        }

        _resultsList!.Subviews.ElementAt(_previewedResultIndex.Value).Border.BorderStyle = BorderStyle.None;
        _previewedResultIndex = null;
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
}