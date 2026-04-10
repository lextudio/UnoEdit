using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextEditor : UserControl
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(
            nameof(Document),
            typeof(TextDocument),
            typeof(TextEditor),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CurrentOffsetProperty =
        DependencyProperty.Register(
            nameof(CurrentOffset),
            typeof(int),
            typeof(TextEditor),
            new PropertyMetadata(0, OnCurrentOffsetChanged));

    public static readonly DependencyProperty SelectionStartOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionStartOffset),
            typeof(int),
            typeof(TextEditor),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty SelectionEndOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionEndOffset),
            typeof(int),
            typeof(TextEditor),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(
            nameof(Theme),
            typeof(TextEditorTheme),
            typeof(TextEditor),
            new PropertyMetadata(TextEditorTheme.Dark, OnThemeChanged));

    private TextDocument? _attachedDocument;

    public TextEditor()
    {
        this.InitializeComponent();
        PART_SearchPanel.Attach(this);
        PART_TextArea.CaretOffsetChanged  += OnTextAreaCaretOffsetChanged;
        PART_TextArea.SelectionChanged    += OnTextAreaSelectionChanged;
        PART_TextArea.NavigationRequested += (s, e) => NavigationRequested?.Invoke(this, e);
        KeyDown += OnEditorKeyDown;
        ApplyThemeToChrome();
    }

    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public int CurrentOffset
    {
        get => (int)GetValue(CurrentOffsetProperty);
        set => SetValue(CurrentOffsetProperty, value);
    }

    public int SelectionStartOffset
    {
        get => (int)GetValue(SelectionStartOffsetProperty);
        set => SetValue(SelectionStartOffsetProperty, value);
    }

    public int SelectionEndOffset
    {
        get => (int)GetValue(SelectionEndOffsetProperty);
        set => SetValue(SelectionEndOffsetProperty, value);
    }

    public TextEditorTheme Theme
    {
        get => (TextEditorTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>Provides access to the inner TextArea for testing and advanced scenarios.</summary>
    public TextArea TextArea => PART_TextArea;

    public SearchPanel SearchPanel => PART_SearchPanel;

    public IReferenceSegmentSource? ReferenceSegmentSource
    {
        get => PART_TextArea.ReferenceSegmentSource;
        set => PART_TextArea.ReferenceSegmentSource = value;
    }

    public ICSharpCode.AvalonEdit.Folding.FoldingManager? FoldingManager
    {
        get => PART_TextArea.FoldingManager;
        set => PART_TextArea.FoldingManager = value;
    }

    /// <summary>Raised when the user Ctrl+Clicks a reference segment.</summary>
    public event EventHandler<ReferenceSegment>? NavigationRequested;

    /// <summary>Scroll the editor viewport so the given 1-based line number is visible.</summary>
    public void ScrollToLine(int lineNumber)
    {
        PART_TextArea.ScrollToLine(lineNumber);
    }

    /// <summary>Scroll the editor viewport so the given offset is visible and move the caret there.</summary>
    public void ScrollToOffset(int offset)
    {
        if (Document is null) return;
        int clamped = Math.Clamp(offset, 0, Document.TextLength);
        CurrentOffset = clamped;
    }

    /// <summary>Set selection and scroll the anchor into view.</summary>
    public void SetSelection(int startOffset, int endOffset)
    {
        if (Document is null) return;
        int len = Document.TextLength;
        SelectionStartOffset = Math.Clamp(Math.Min(startOffset, endOffset), 0, len);
        SelectionEndOffset   = Math.Clamp(Math.Max(startOffset, endOffset), 0, len);
        CurrentOffset        = SelectionEndOffset;
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
        editor.PART_TextArea.Document = args.NewValue as TextDocument;
        editor.PART_SearchPanel.UpdateDocument(args.NewValue as TextDocument);
        editor.UpdateSummary();
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        var theme = (args.NewValue as TextEditorTheme) ?? TextEditorTheme.Dark;
        editor.PART_TextArea.Theme = theme;
        editor.PART_SearchPanel.UpdateTheme(theme);
        editor.ApplyThemeToChrome();
    }

    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        EditorBorder.Background         = new SolidColorBrush(t.EditorBackground);
        EditorBorder.BorderBrush        = new SolidColorBrush(t.BorderColor);
        TitleBarGrid.Background         = new SolidColorBrush(t.TitleBarBackground);
        TitleTextBlock.Foreground       = new SolidColorBrush(t.TitleBarForeground);
        SummaryTextBlock.Foreground     = new SolidColorBrush(t.GutterForeground);
    }

    private static void OnCurrentOffsetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        if (editor.PART_TextArea.CurrentOffset != (int)args.NewValue)
        {
            editor.PART_TextArea.CurrentOffset = (int)args.NewValue;
        }

        editor.UpdateSummary();
    }

    private static void OnSelectionRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        if (editor.PART_TextArea.SelectionStartOffset != editor.SelectionStartOffset)
        {
            editor.PART_TextArea.SelectionStartOffset = editor.SelectionStartOffset;
        }

        if (editor.PART_TextArea.SelectionEndOffset != editor.SelectionEndOffset)
        {
            editor.PART_TextArea.SelectionEndOffset = editor.SelectionEndOffset;
        }

        editor.UpdateSummary();
    }

    private void AttachDocument(TextDocument? oldDocument, TextDocument? newDocument)
    {
        if (oldDocument is not null)
        {
            oldDocument.TextChanged -= OnDocumentTextChanged;
        }

        _attachedDocument = newDocument;

        if (newDocument is not null)
        {
            newDocument.TextChanged += OnDocumentTextChanged;
        }
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        PART_SearchPanel.RefreshSearch();
        UpdateSummary();
    }

    private void OnTextAreaCaretOffsetChanged(object? sender, EventArgs e)
    {
        if (CurrentOffset != PART_TextArea.CurrentOffset)
        {
            CurrentOffset = PART_TextArea.CurrentOffset;
        }

        UpdateSummary();
    }

    private void OnTextAreaSelectionChanged(object? sender, EventArgs e)
    {
        if (SelectionStartOffset != PART_TextArea.SelectionStartOffset)
        {
            SelectionStartOffset = PART_TextArea.SelectionStartOffset;
        }

        if (SelectionEndOffset != PART_TextArea.SelectionEndOffset)
        {
            SelectionEndOffset = PART_TextArea.SelectionEndOffset;
        }

        UpdateSummary();
    }

    internal void UpdateSummary()
    {
        if (Document is null)
        {
            SummaryTextBlock.Text = "No document";
            return;
        }

        TextLocation location = Document.GetLocation(CurrentOffset);
        int selectionLength = Math.Abs(SelectionEndOffset - SelectionStartOffset);
        string selectionSummary = selectionLength > 0 ? $"  Sel {selectionLength}" : string.Empty;
        SummaryTextBlock.Text = $"{Document.LineCount} lines  {Document.TextLength} chars  Ln {location.Line}, Col {location.Column}{selectionSummary}";
    }

    public void OpenSearchPanel()
    {
        PART_SearchPanel.Open(GetSelectedTextOrNull());
    }

    public void CloseSearchPanel()
    {
        PART_SearchPanel.Close();
    }

    public void FindNext()
    {
        if (!PART_SearchPanel.IsOpen)
        {
            PART_SearchPanel.Open();
        }

        PART_SearchPanel.FindNext();
    }

    public void FindPrevious()
    {
        if (!PART_SearchPanel.IsOpen)
        {
            PART_SearchPanel.Open();
        }

        PART_SearchPanel.FindPrevious();
    }

    private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool controlPressed = IsControlPressed();
        bool shiftPressed = IsShiftPressed();

        if (controlPressed && e.Key == Windows.System.VirtualKey.F)
        {
            OpenSearchPanel();
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.F3)
        {
            if (shiftPressed)
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

        if (e.Key == Windows.System.VirtualKey.Escape && PART_SearchPanel.IsOpen)
        {
            CloseSearchPanel();
            e.Handled = true;
        }
    }

    private string? GetSelectedTextOrNull()
    {
        if (Document is null || SelectionStartOffset == SelectionEndOffset)
        {
            return null;
        }

        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset = Math.Max(SelectionStartOffset, SelectionEndOffset);
        if (endOffset <= startOffset)
        {
            return null;
        }

        string text = Document.GetText(startOffset, endOffset - startOffset);
        return text.Contains('\n') ? null : text;
    }

    private static bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static bool IsControlPressed()
    {
        var flags = Windows.UI.Core.CoreVirtualKeyStates.Down;
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftWindows).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightWindows).HasFlag(flags);
    }
}
