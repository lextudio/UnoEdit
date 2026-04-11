using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        RaiseSearchOptionsChanged();
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

    private void RaiseSearchOptionsChanged()
    {
        SearchOptionsChanged?.Invoke(this,
            new SearchOptionsChangedEventArgs(
                SearchPattern,
                MatchCaseCheckBox.IsChecked == true,
                UseRegexCheckBox.IsChecked == true,
                WholeWordsCheckBox.IsChecked == true));
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

    // ----------------------------------------------------------------
    // AvalonEdit API surface stubs
    // ----------------------------------------------------------------
    public bool IsClosed => !IsOpen;
    public void Reactivate() { Open(); }
    public void Uninstall() { Close(); }
    public static SearchPanel Install(object textArea)
    {
        if (textArea == null)
            throw new ArgumentNullException(nameof(textArea));

        // Prefer TextEditor, which already owns a SearchPanel instance.
        if (textArea is TextEditor editor)
            return editor.SearchPanel;

        // If called with a TextArea, try to navigate back to owning editor via Parent chain.
        if (textArea is FrameworkElement fe) {
            var current = fe;
            while (current != null) {
                if (current is TextEditor hostEditor)
                    return hostEditor.SearchPanel;
                current = current.Parent as FrameworkElement;
            }
        }

        // Best-effort fallback for test doubles exposing SearchPanel property.
        var panelProperty = textArea.GetType().GetProperty("SearchPanel", BindingFlags.Public | BindingFlags.Instance);
        if (panelProperty?.GetValue(textArea) is SearchPanel existing)
            return existing;

        throw new ArgumentException("Could not locate a SearchPanel for the provided text area/editor.", nameof(textArea));
    }

    public static void RegisterCommands(Microsoft.UI.Xaml.Input.KeyboardAccelerator accelerators)
    {
        // Uno keyboard accelerators are typically registered at control level in TextEditor.
        // Keep API compatibility and no-op when called from existing parity tests.
    }

    public static readonly DependencyProperty SearchPatternProperty =
        DependencyProperty.Register(nameof(SearchPattern), typeof(string), typeof(SearchPanel), new PropertyMetadata(""));

    public static readonly DependencyProperty MatchCaseProperty =
        DependencyProperty.Register(nameof(MatchCase), typeof(bool), typeof(SearchPanel), new PropertyMetadata(false));
    public bool MatchCase {
        get => (bool)GetValue(MatchCaseProperty);
        set => SetValue(MatchCaseProperty, value);
    }

    public static readonly DependencyProperty UseRegexProperty =
        DependencyProperty.Register(nameof(UseRegex), typeof(bool), typeof(SearchPanel), new PropertyMetadata(false));
    public bool UseRegex {
        get => (bool)GetValue(UseRegexProperty);
        set => SetValue(UseRegexProperty, value);
    }

    public static readonly DependencyProperty WholeWordsProperty =
        DependencyProperty.Register(nameof(WholeWords), typeof(bool), typeof(SearchPanel), new PropertyMetadata(false));
    public bool WholeWords {
        get => (bool)GetValue(WholeWordsProperty);
        set => SetValue(WholeWordsProperty, value);
    }

    public static readonly DependencyProperty LocalizationProperty =
        DependencyProperty.Register(nameof(Localization), typeof(Localization), typeof(SearchPanel), new PropertyMetadata(null));
    public Localization Localization {
        get => (Localization)GetValue(LocalizationProperty);
        set => SetValue(LocalizationProperty, value);
    }

    public static readonly DependencyProperty MarkerBrushProperty =
        DependencyProperty.Register(nameof(MarkerBrush), typeof(Microsoft.UI.Xaml.Media.Brush), typeof(SearchPanel), new PropertyMetadata(null));
    public Microsoft.UI.Xaml.Media.Brush MarkerBrush {
        get => (Microsoft.UI.Xaml.Media.Brush)GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    public static readonly DependencyProperty MarkerPenProperty =
        DependencyProperty.Register(nameof(MarkerPen), typeof(System.Windows.Media.Pen), typeof(SearchPanel), new PropertyMetadata(null));
    public System.Windows.Media.Pen MarkerPen {
        get => (System.Windows.Media.Pen)GetValue(MarkerPenProperty);
        set => SetValue(MarkerPenProperty, value);
    }

    public static readonly DependencyProperty MarkerCornerRadiusProperty =
        DependencyProperty.Register(nameof(MarkerCornerRadius), typeof(double), typeof(SearchPanel), new PropertyMetadata(3.0));
    public double MarkerCornerRadius {
        get => (double)GetValue(MarkerCornerRadiusProperty);
        set => SetValue(MarkerCornerRadiusProperty, value);
    }

    public event EventHandler<ICSharpCode.AvalonEdit.Search.SearchOptionsChangedEventArgs> SearchOptionsChanged;

	public new void OnApplyTemplate() { base.OnApplyTemplate(); }
}
