using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class SearchPanel : UserControl
{
    private TextEditor? _editor;
    private TextDocument? _attachedDocument;
    private List<ISearchResult> _results = new();
    private int _selectedResultIndex = -1;

    public SearchPanel()
    {
        this.InitializeComponent();
    }

    public bool IsOpen => Visibility == Visibility.Visible;

    public string SearchPattern
    {
        get => SearchTextBox.Text ?? string.Empty;
        set => SearchTextBox.Text = value ?? string.Empty;
    }

    public void Attach(TextEditor editor)
    {
        _editor = editor;
    }

    public void UpdateTheme(TextEditorTheme theme)
    {
        RootBorder.Background = new SolidColorBrush(theme.TitleBarBackground);
        RootBorder.BorderBrush = new SolidColorBrush(theme.BorderColor);
        ResultsTextBlock.Foreground = new SolidColorBrush(theme.GutterForeground);
    }

    public void UpdateDocument(TextDocument? document)
    {
        if (_attachedDocument is not null)
        {
            _attachedDocument.TextChanged -= OnDocumentTextChanged;
        }

        _attachedDocument = document;

        if (document is not null)
        {
            document.TextChanged += OnDocumentTextChanged;
        }

        RefreshSearch(selectBestMatch: false);
    }

    public void Open(string? initialPattern = null)
    {
        if (!string.IsNullOrEmpty(initialPattern))
        {
            SearchPattern = initialPattern;
        }

        Visibility = Visibility.Visible;
        _selectedResultIndex = -1;
        RefreshSearch(selectBestMatch: false);
        SearchTextBox.Focus(FocusState.Programmatic);
        SearchTextBox.SelectAll();
    }

    public void Close()
    {
        Visibility = Visibility.Collapsed;
    }

    public void FindNext()
    {
        if (!EnsureSearchResults())
        {
            return;
        }

        if (_selectedResultIndex < 0)
        {
            _selectedResultIndex = FindResultIndexAtOrAfter(CurrentOffsetOrZero());
        }
        else
        {
            _selectedResultIndex = (_selectedResultIndex + 1) % _results.Count;
        }

        SelectCurrentResult();
    }

    public void FindPrevious()
    {
        if (!EnsureSearchResults())
        {
            return;
        }

        if (_selectedResultIndex < 0)
        {
            int currentOffset = CurrentOffsetOrZero();
            _selectedResultIndex = _results.FindLastIndex(result => result.Offset < currentOffset);
            if (_selectedResultIndex < 0)
            {
                _selectedResultIndex = _results.Count - 1;
            }
        }
        else
        {
            _selectedResultIndex = (_selectedResultIndex - 1 + _results.Count) % _results.Count;
        }

        SelectCurrentResult();
    }

    public void RefreshSearch(bool selectBestMatch = false)
    {
        string pattern = SearchPattern;
        TextDocument? document = _attachedDocument;

        if (document is null || string.IsNullOrEmpty(pattern))
        {
            _results.Clear();
            _selectedResultIndex = -1;
            UpdateResultsText();
            return;
        }

        try
        {
            ISearchStrategy strategy = SearchStrategyFactory.Create(
                pattern,
                ignoreCase: MatchCaseCheckBox.IsChecked != true,
                matchWholeWords: WholeWordsCheckBox.IsChecked == true,
                mode: UseRegexCheckBox.IsChecked == true ? SearchMode.RegEx : SearchMode.Normal);

            _results = strategy.FindAll(document, 0, document.TextLength).ToList();
            if (selectBestMatch)
            {
                _selectedResultIndex = FindResultIndexAtOrAfter(CurrentOffsetOrZero());
                if (_selectedResultIndex >= 0)
                {
                    SelectCurrentResult();
                    return;
                }
            }
            else if (_selectedResultIndex >= _results.Count)
            {
                _selectedResultIndex = _results.Count - 1;
            }

            UpdateResultsText();
        }
        catch (SearchPatternException ex)
        {
            _results.Clear();
            _selectedResultIndex = -1;
            ResultsTextBlock.Text = ex.Message;
        }
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        RefreshSearch(selectBestMatch: false);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _selectedResultIndex = -1;
        RefreshSearch(selectBestMatch: false);
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        _selectedResultIndex = -1;
        RefreshSearch(selectBestMatch: false);
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e)
    {
        FindPrevious();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSearchTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (IsShiftPressed())
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Close();
            _editor?.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private bool EnsureSearchResults()
    {
        if (_results.Count > 0)
        {
            return true;
        }

        RefreshSearch(selectBestMatch: true);
        return _results.Count > 0;
    }

    private int FindResultIndexAtOrAfter(int offset)
    {
        for (int index = 0; index < _results.Count; index++)
        {
            if (_results[index].Offset >= offset)
            {
                return index;
            }
        }

        return _results.Count > 0 ? 0 : -1;
    }

    private int CurrentOffsetOrZero()
    {
        return _editor?.CurrentOffset ?? 0;
    }

    private void SelectCurrentResult()
    {
        if (_editor is null || _selectedResultIndex < 0 || _selectedResultIndex >= _results.Count)
        {
            UpdateResultsText();
            return;
        }

        ISearchResult result = _results[_selectedResultIndex];
        _editor.SetSelection(result.Offset, result.EndOffset);
        _editor.ScrollToOffset(result.Offset);
        UpdateResultsText();
    }

    private void UpdateResultsText()
    {
        if (_results.Count == 0)
        {
            ResultsTextBlock.Text = string.IsNullOrEmpty(SearchPattern) ? "Find" : "No results";
            return;
        }

        int current = _selectedResultIndex >= 0 ? _selectedResultIndex + 1 : 0;
        ResultsTextBlock.Text = $"{current}/{_results.Count}";
    }

    private static bool IsShiftPressed()
    {
        return Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}
