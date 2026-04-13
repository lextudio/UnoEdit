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
    private ISearchPanelHost? _editor;
    private TextDocument? _attachedDocument;
    private List<ISearchResult> _results = new();
    private int _selectedResultIndex = -1;

    public SearchPanel()
    {
        this.InitializeComponent();
        SearchTextBox.GotFocus += (_, _) =>
        {
            var fgBrush = SearchTextBox.Foreground as SolidColorBrush;
            var bgBrush = SearchTextBox.Background as SolidColorBrush;
            Console.WriteLine($"[SearchTextBox.GotFocus] Foreground={fgBrush?.Color} Background={bgBrush?.Color}");
        };
    }

    public bool IsOpen => Visibility == Visibility.Visible;

    public string SearchPattern
    {
        get => SearchTextBox.Text ?? string.Empty;
        set => SearchTextBox.Text = value ?? string.Empty;
    }

    public void Attach(ISearchPanelHost editor)
    {
        _editor = editor;
    }

    public void UpdateTheme(TextEditorTheme theme)
    {
        var bg = new SolidColorBrush(theme.TitleBarBackground);
        var border = new SolidColorBrush(theme.BorderColor);
        var fg = new SolidColorBrush(theme.TitleBarForeground);
        var boxBorder = new SolidColorBrush(theme.GutterForeground);
        var placeholder = new SolidColorBrush(theme.GutterForeground);
        var results = new SolidColorBrush(theme.GutterForeground);
        var textBoxBg = new SolidColorBrush(theme.EditorBackground);
        bool isDark = theme == TextEditorTheme.Dark;

        RootBorder.Background = bg;
        RootBorder.BorderBrush = border;
        SearchTextBox.Foreground = fg;
        SearchTextBox.PlaceholderForeground = placeholder;
        SearchTextBox.Background = textBoxBg;

        // TextBox border / underline and background resource overrides via RequestedTheme.
        // ThemeResource in the TextBox ControlTemplate resolves from ThemeDictionaries in
        // ancestor elements, not from flat Resources. We define our custom keys in
        // SearchPanel's UserControl.Resources ThemeDictionaries (SearchPanel.xaml).
        SearchTextBox.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        // Remove stale flat-resource overrides — ThemeDictionaries above take priority
        this.Resources.Remove("TextControlBackgroundFocused");
        this.Resources.Remove("TextControlForegroundFocused");
        this.Resources.Remove("TextControlBackgroundPointerOver");
        // Remove per-element overrides too (ThemeDictionaries approach supersedes them)
        foreach (var key in new[] { "TextControlBackground", "TextControlBackgroundPointerOver",
                                    "TextControlBackgroundFocused", "TextControlForegroundFocused",
                                    "TextControlBorderBrush", "TextControlBorderBrushPointerOver",
                                    "TextControlBorderBrushFocused" })
        {
            SearchTextBox.Resources.Remove(key);
        }

        Console.WriteLine($"[SearchPanel.UpdateTheme] theme={theme.Name}");
        Console.WriteLine($"  SearchTextBox.Foreground = {fg.Color}");
        Console.WriteLine($"  SearchTextBox.Background = {textBoxBg.Color}");
        Console.WriteLine($"  TextControlBackground    = {textBoxBg.Color}");
        Console.WriteLine($"  TextControlBackgroundFocused = {textBoxBg.Color}");
        Console.WriteLine($"  TextControlForegroundFocused = {fg.Color}");

        PreviousButton.Foreground = fg;
        NextButton.Foreground = fg;
        CloseButton.Foreground = fg;

        // Checkbox box border + hover/pressed states (overrides local resource keys set in XAML)
        var boxBorderHover = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0xCB, 0xD5, 0xE1)   // #CBD5E1
            : Windows.UI.Color.FromArgb(0xFF, 0x37, 0x41, 0x51));  // #374151
        var boxFillHover = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0x22, 0x33, 0x41, 0x55)    // #22334155
            : Windows.UI.Color.FromArgb(0x22, 0x09, 0x69, 0xDA));  // #220969DA

        foreach (var cb in new[] { MatchCaseCheckBox, WholeWordsCheckBox, UseRegexCheckBox })
        {
            cb.Resources["CheckBoxCheckBackgroundStrokeUnchecked"] = boxBorder;
            cb.Resources["CheckBoxCheckBackgroundStrokeUncheckedPointerOver"] = boxBorderHover;
            cb.Resources["CheckBoxCheckBackgroundStrokeUncheckedPressed"] = boxBorder;
            cb.Resources["CheckBoxCheckBackgroundStrokeChecked"] = boxBorder;
            cb.Resources["CheckBoxCheckBackgroundStrokeCheckedPointerOver"] = boxBorderHover;
            cb.Resources["CheckBoxCheckBackgroundStrokeCheckedPressed"] = boxBorder;
            cb.Resources["CheckBoxCheckBackgroundFillUncheckedPointerOver"] = boxFillHover;
        }

        // Button hover/pressed foreground and background
        var btnFgHover = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0xE5, 0xE7, 0xEB)    // #E5E7EB
            : Windows.UI.Color.FromArgb(0xFF, 0x11, 0x18, 0x27));  // #111827
        var btnFgPressed = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0xCB, 0xD5, 0xE1)    // #CBD5E1
            : Windows.UI.Color.FromArgb(0xFF, 0x37, 0x41, 0x51));  // #374151
        var btnBgHover = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0x33, 0x41, 0x55)    // #334155
            : Windows.UI.Color.FromArgb(0xFF, 0xD1, 0xD5, 0xDB));  // #D1D5DB
        var btnBgPressed = new SolidColorBrush(theme == TextEditorTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0x47, 0x56, 0x69)    // #475569
            : Windows.UI.Color.FromArgb(0xFF, 0xE5, 0xE7, 0xEB));  // #E5E7EB

        foreach (var btn in new[] { PreviousButton, NextButton, CloseButton })
        {
            btn.Resources["ButtonForegroundPointerOver"] = btnFgHover;
            btn.Resources["ButtonForegroundPressed"] = btnFgPressed;
            btn.Resources["ButtonBackgroundPointerOver"] = btnBgHover;
            btn.Resources["ButtonBackgroundPressed"] = btnBgPressed;
        }

        // Label TextBlocks are named elements — set directly
        MatchCaseLabel.Foreground = fg;
        WholeWordsLabel.Foreground = fg;
        UseRegexLabel.Foreground = fg;

        ResultsTextBlock.Foreground = results;
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
    // AvalonEdit API compatibility surface
    // ----------------------------------------------------------------
    public bool IsClosed => !IsOpen;
    public void Reactivate()
    {
        if (!IsOpen)
        {
            Open();
        }
        else
        {
            SearchTextBox.Focus(FocusState.Programmatic);
            SearchTextBox.SelectAll();
        }
    }

    public void Uninstall()
    {
        Close();
        if (_attachedDocument is not null)
        {
            _attachedDocument.TextChanged -= OnDocumentTextChanged;
            _attachedDocument = null;
        }
        _results.Clear();
        _selectedResultIndex = -1;
    }
    public static SearchPanel Install(object textArea)
    {
        if (textArea == null)
            throw new ArgumentNullException(nameof(textArea));

        // Prefer ISearchPanelHost (both Uno and WinUI TextEditor implement it).
        if (textArea is ISearchPanelHost searchHost)
            return searchHost.SearchPanel;

        // If called with a TextArea, try to navigate back to owning editor via Parent chain.
        if (textArea is FrameworkElement fe) {
            var current = fe;
            while (current != null) {
                if (current is ISearchPanelHost hostEditor)
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

    public void RegisterCommands(System.Windows.Input.CommandBindingCollection commandBindings)
    {
        ArgumentNullException.ThrowIfNull(commandBindings);
        commandBindings.Add(SearchCommands.FindNext, (_, __) => FindNext());
        commandBindings.Add(SearchCommands.FindPrevious, (_, __) => FindPrevious());
        commandBindings.Add(SearchCommands.CloseSearchPanel, (_, __) => Close());
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
