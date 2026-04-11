using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media;

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

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options),
            typeof(TextEditorOptions),
            typeof(TextEditor),
            new PropertyMetadata(new TextEditorOptions(), OnOptionsChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(TextEditor),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(TextEditor),
            new PropertyMetadata(false, OnIsModifiedChanged));

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowLineNumbers),
            typeof(bool),
            typeof(TextEditor),
            new PropertyMetadata(true, OnShowLineNumbersChanged));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(
            nameof(WordWrap),
            typeof(bool),
            typeof(TextEditor),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty LineNumbersForegroundProperty =
        DependencyProperty.Register(
            nameof(LineNumbersForeground),
            typeof(Brush),
            typeof(TextEditor),
            new PropertyMetadata(null, OnLineNumbersForegroundChanged));

    public static readonly DependencyProperty SyntaxHighlightingProperty =
        DependencyProperty.Register(
            nameof(SyntaxHighlighting),
            typeof(IHighlightingDefinition),
            typeof(TextEditor),
            new PropertyMetadata(null, OnSyntaxHighlightingChanged));

    public static readonly DependencyProperty EncodingProperty =
        DependencyProperty.Register(
            nameof(Encoding),
            typeof(Encoding),
            typeof(TextEditor),
            new PropertyMetadata(Encoding.UTF8));

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

    public TextEditorOptions Options
    {
        get => (TextEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    public bool ShowLineNumbers
    {
        get => (bool)GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    public bool WordWrap
    {
        get => (bool)GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    public Brush? LineNumbersForeground
    {
        get => (Brush?)GetValue(LineNumbersForegroundProperty);
        set => SetValue(LineNumbersForegroundProperty, value);
    }

    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => (IHighlightingDefinition?)GetValue(SyntaxHighlightingProperty);
        set => SetValue(SyntaxHighlightingProperty, value);
    }

    public Encoding Encoding
    {
        get => (Encoding)GetValue(EncodingProperty);
        set => SetValue(EncodingProperty, value);
    }

    public string Text
    {
        get => Document?.Text ?? string.Empty;
        set
        {
            var document = EnsureDocument();
            document.Text = value ?? string.Empty;
            CurrentOffset = 0;
            SelectionStartOffset = 0;
            SelectionEndOffset = 0;
            document.UndoStack.ClearAll();
        }
    }

    public int CaretOffset
    {
        get => CurrentOffset;
        set => CurrentOffset = value;
    }

    public int SelectionStart
    {
        get => SelectionLength == 0 ? CurrentOffset : Math.Min(SelectionStartOffset, SelectionEndOffset);
        set => Select(value, SelectionLength);
    }

    public int SelectionLength
    {
        get => Math.Abs(SelectionEndOffset - SelectionStartOffset);
        set => Select(SelectionStart, value);
    }

    public string SelectedText
    {
        get
        {
            if (Document is null || SelectionLength == 0)
            {
                return string.Empty;
            }

            int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
            return Document.GetText(startOffset, SelectionLength);
        }
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var document = EnsureDocument();
            int startOffset = SelectionStart;
            int selectionLength = SelectionLength;
            document.Replace(startOffset, selectionLength, value);
            Select(startOffset, value.Length);
        }
    }

    public int LineCount => Document?.LineCount ?? 1;

    public bool CanUndo => Document?.UndoStack.CanUndo ?? false;

    public bool CanRedo => Document?.UndoStack.CanRedo ?? false;

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

    public IHighlightedLineSource? HighlightedLineSource
    {
        get => PART_TextArea.HighlightedLineSource;
        set => PART_TextArea.HighlightedLineSource = value;
    }

    public event EventHandler? DocumentChanged;

    public event EventHandler? TextChanged;

    public event PropertyChangedEventHandler? OptionChanged;

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

    public void Select(int start, int length)
    {
        var document = Document;
        int documentLength = document?.TextLength ?? 0;
        if (start < 0 || start > documentLength)
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Value must be between 0 and {documentLength}");
        if (length < 0 || start + length > documentLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Value must be between 0 and {documentLength - start}");

        SetSelection(start, start + length);
    }

    public void AppendText(string textData)
    {
        var document = EnsureDocument();
        document.Insert(document.TextLength, textData ?? string.Empty);
    }

    public void BeginChange()
    {
        EnsureDocument().BeginUpdate();
    }

    public IDisposable DeclareChangeBlock()
    {
        return EnsureDocument().RunUpdate();
    }

    public void EndChange()
    {
        EnsureDocument().EndUpdate();
    }

    public void Clear()
    {
        Text = string.Empty;
    }

    public void Undo()
    {
        if (Document?.UndoStack.CanUndo == true)
        {
            Document.UndoStack.Undo();
        }
    }

    public void Redo()
    {
        if (Document?.UndoStack.CanRedo == true)
        {
            Document.UndoStack.Redo();
        }
    }

    public void Load(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var reader = new StreamReader(stream, Encoding ?? Encoding.UTF8, true, 1024, leaveOpen: true);
        Text = reader.ReadToEnd();
        Encoding = reader.CurrentEncoding;
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Load(string fileName)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        using FileStream fs = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        Load(fs);
    }

    public void Save(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var writer = new StreamWriter(stream, Encoding ?? Encoding.UTF8, 1024, leaveOpen: true);
        if (Document is not null)
        {
            Document.WriteTextTo(writer);
        }
        else
        {
            writer.Write(string.Empty);
        }
        writer.Flush();
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Save(string fileName)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        Save(fs);
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
        editor.PART_TextArea.Document = args.NewValue as TextDocument;
        editor.PART_SearchPanel.UpdateDocument(args.NewValue as TextDocument);
        editor.UpdateSummary();
        editor.DocumentChanged?.Invoke(editor, EventArgs.Empty);
        editor.TextChanged?.Invoke(editor, EventArgs.Empty);
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        var theme = (args.NewValue as TextEditorTheme) ?? TextEditorTheme.Dark;
        editor.PART_TextArea.Theme = theme;
        editor.PART_SearchPanel.UpdateTheme(theme);
        editor.ApplyThemeToChrome();
    }

    private static void OnOptionsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.Options = (args.NewValue as TextEditorOptions) ?? new TextEditorOptions();
        editor.OptionChanged?.Invoke(editor, new PropertyChangedEventArgs(null));
    }

    private static void OnIsReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.IsReadOnly = (bool)args.NewValue;
    }

    private static void OnIsModifiedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        var document = editor.Document;
        if (document is null)
        {
            return;
        }

        if ((bool)args.NewValue)
        {
            if (document.UndoStack.IsOriginalFile)
            {
                document.UndoStack.DiscardOriginalFileMarker();
            }
        }
        else
        {
            document.UndoStack.MarkAsOriginalFile();
        }
    }

    private static void OnShowLineNumbersChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.ShowLineNumbers = (bool)args.NewValue;
    }

    private static void OnWordWrapChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.WordWrap = (bool)args.NewValue;
    }

    private static void OnLineNumbersForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.LineNumbersForeground = args.NewValue as Brush;
        if (args.NewValue is Brush brush)
        {
            editor.SummaryTextBlock.Foreground = brush;
        }
        else
        {
            editor.ApplyThemeToChrome();
        }
    }

    private static void OnSyntaxHighlightingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.SyntaxHighlighting = args.NewValue as IHighlightingDefinition;
    }

    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        EditorBorder.Background         = new SolidColorBrush(t.EditorBackground);
        EditorBorder.BorderBrush        = new SolidColorBrush(t.BorderColor);
        TitleBarGrid.Background         = new SolidColorBrush(t.TitleBarBackground);
        TitleTextBlock.Foreground       = new SolidColorBrush(t.TitleBarForeground);
        SummaryTextBlock.Foreground     = LineNumbersForeground ?? new SolidColorBrush(t.GutterForeground);
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
            oldDocument.UndoStack.PropertyChanged -= OnUndoStackPropertyChanged;
        }

        _attachedDocument = newDocument;

        if (newDocument is not null)
        {
            newDocument.TextChanged += OnDocumentTextChanged;
            newDocument.UndoStack.PropertyChanged += OnUndoStackPropertyChanged;
        }

        SyncIsModifiedFromDocument();
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        PART_SearchPanel.RefreshSearch();
        UpdateSummary();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnUndoStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(ICSharpCode.AvalonEdit.Document.UndoStack.IsOriginalFile))
        {
            SyncIsModifiedFromDocument();
        }
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

    private TextDocument EnsureDocument()
    {
        if (Document is null)
        {
            Document = new TextDocument();
        }

        return Document;
    }

    private void SyncIsModifiedFromDocument()
    {
        bool isModified = Document is not null && !Document.UndoStack.IsOriginalFile;
        if (IsModified != isModified)
        {
            SetValue(IsModifiedProperty, isModified);
        }
    }

    // ----------------------------------------------------------------
    // Clipboard operations
    // ----------------------------------------------------------------
    public void Copy()
    {
        if (SelectionLength == 0)
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(SelectedText);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }

    public void Cut()
    {
        if (IsReadOnly || SelectionLength == 0)
        {
            return;
        }

        Copy();
        DeleteSelection();
    }

    public void Delete()
    {
        if (IsReadOnly)
        {
            return;
        }

        if (SelectionLength > 0)
        {
            DeleteSelection();
            return;
        }

        var document = Document;
        if (document is null)
        {
            return;
        }

        int offset = Math.Clamp(CurrentOffset, 0, document.TextLength);
        if (offset < document.TextLength)
        {
            document.Remove(offset, 1);
            Select(offset, 0);
        }
    }

    public void Paste()
    {
        if (IsReadOnly)
        {
            return;
        }

        _ = PasteAsync();
    }

    public void SelectAll() { Select(0, Document?.TextLength ?? 0); }

    // ----------------------------------------------------------------
    // Scroll methods
    // ----------------------------------------------------------------
    public void LineUp() { ScrollCaretByLines(-1, false); }
    public void LineDown() { ScrollCaretByLines(1, false); }
    public void LineLeft() { ScrollHorizontalBy(-32); }
    public void LineRight() { ScrollHorizontalBy(32); }
    public void PageUp() { ScrollCaretByLines(-GetApproxPageLineCount(), true); }
    public void PageDown() { ScrollCaretByLines(GetApproxPageLineCount(), true); }
    public void PageLeft() { ScrollHorizontalBy(-(ViewportWidth > 0 ? ViewportWidth : 240)); }
    public void PageRight() { ScrollHorizontalBy(ViewportWidth > 0 ? ViewportWidth : 240); }
    public void ScrollTo(int line, int column) { ScrollToLine(line); }
    public void ScrollToEnd()
    {
        if (Document is null)
        {
            return;
        }

        CurrentOffset = Document.TextLength;
        ScrollToOffset(CurrentOffset);
    }

    public void ScrollToHome()
    {
        CurrentOffset = 0;
        ScrollToOffset(0);
    }

    public void ScrollToHorizontalOffset(double offset)
    {
        TryChangeView(offset, null, true);
    }

    public void ScrollToVerticalOffset(double offset)
    {
        TryChangeView(null, offset, true);
    }

    // ----------------------------------------------------------------
    // Scroll position / size properties
    // ----------------------------------------------------------------
    public double HorizontalOffset => GetScrollViewerMetric("HorizontalOffset");
    public double VerticalOffset => GetScrollViewerMetric("VerticalOffset");
    public double ExtentHeight => GetScrollViewerMetric("ExtentHeight");
    public double ExtentWidth => GetScrollViewerMetric("ExtentWidth");
    public double ViewportHeight => GetScrollViewerMetric("ViewportHeight", ActualHeight);
    public double ViewportWidth => GetScrollViewerMetric("ViewportWidth", ActualWidth);

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));
    public ScrollBarVisibility HorizontalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));
    public ScrollBarVisibility VerticalScrollBarVisibility {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    // ----------------------------------------------------------------
    // Position lookup
    // ----------------------------------------------------------------
    public TextViewPosition? GetPositionFromPoint(Windows.Foundation.Point point)
    {
        var document = Document;
        if (document is null)
        {
            return null;
        }

        var textView = GetInnerTextView();
        if (textView == null)
        {
            return null;
        }

        var method = textView.GetType().GetMethod("GetOffsetFromViewPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            return null;
        }

        var offsetObj = method.Invoke(textView, new object[] { point.X, point.Y });
        if (offsetObj is not int offset)
        {
            return null;
        }

        offset = Math.Clamp(offset, 0, document.TextLength);
        var location = document.GetLocation(offset);
        return new TextViewPosition(location.Line, location.Column);
    }

    // ----------------------------------------------------------------
    // Mouse hover events
    // ----------------------------------------------------------------
    public static readonly object MouseHoverEvent = null;
    public event EventHandler MouseHover;
    public static readonly object MouseHoverStoppedEvent = null;
    public event EventHandler MouseHoverStopped;
    public static readonly object PreviewMouseHoverEvent = null;
    public event EventHandler PreviewMouseHover;
    public static readonly object PreviewMouseHoverStoppedEvent = null;
    public event EventHandler PreviewMouseHoverStopped;

    // raise helpers to avoid unused warning
    void RaiseMouseHover(EventArgs e) { MouseHover?.Invoke(this, e); }
    void RaiseMouseHoverStopped(EventArgs e) { MouseHoverStopped?.Invoke(this, e); }
    void RaisePreviewMouseHover(EventArgs e) { PreviewMouseHover?.Invoke(this, e); }
    void RaisePreviewMouseHoverStopped(EventArgs e) { PreviewMouseHoverStopped?.Invoke(this, e); }

	public new void OnApplyTemplate() { base.OnApplyTemplate(); }

    private async Task PasteAsync()
    {
        var view = Clipboard.GetContent();
        if (view == null || !view.Contains(StandardDataFormats.Text))
        {
            return;
        }

        var text = await view.GetTextAsync();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        SelectedText = text;
        CurrentOffset = SelectionStart + text.Length;
        Select(CurrentOffset, 0);
    }

    private void DeleteSelection()
    {
        if (SelectionLength == 0 || Document is null)
        {
            return;
        }

        int start = SelectionStart;
        Document.Remove(start, SelectionLength);
        Select(start, 0);
    }

    private int GetApproxPageLineCount()
    {
        double lineHeight = FontSize > 0 ? FontSize * 1.4 : 20.0;
        return Math.Max(1, (int)Math.Round((ViewportHeight > 0 ? ViewportHeight : ActualHeight) / lineHeight));
    }

    private void ScrollCaretByLines(int lineDelta, bool moveCaret)
    {
        if (Document is null)
        {
            return;
        }

        var current = Document.GetLocation(Math.Clamp(CurrentOffset, 0, Document.TextLength));
        int targetLine = Math.Clamp(current.Line + lineDelta, 1, Document.LineCount);
        if (moveCaret)
        {
            int targetOffset = Document.GetOffset(targetLine, current.Column);
            CurrentOffset = targetOffset;
        }
        ScrollToLine(targetLine);
    }

    private void ScrollHorizontalBy(double delta)
    {
        double current = HorizontalOffset;
        ScrollToHorizontalOffset(Math.Max(0, current + delta));
    }

    private object? GetInnerTextView()
    {
        var property = PART_TextArea.GetType().GetProperty("PART_TextView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(PART_TextArea);
    }

    private object? GetInnerScrollViewer()
    {
        var textView = GetInnerTextView();
        if (textView == null)
        {
            return null;
        }

        var property = textView.GetType().GetProperty("TextScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(textView);
    }

    private double GetScrollViewerMetric(string name, double fallback = 0)
    {
        var scrollViewer = GetInnerScrollViewer();
        var value = scrollViewer?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(scrollViewer);
        return value is double d ? d : fallback;
    }

    private void TryChangeView(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
    {
        var scrollViewer = GetInnerScrollViewer();
        var method = scrollViewer?.GetType().GetMethod("ChangeView", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            return;
        }

        method.Invoke(scrollViewer, new object[] { horizontalOffset, verticalOffset, null, disableAnimation });
    }
}
