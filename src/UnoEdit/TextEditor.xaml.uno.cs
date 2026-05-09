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

public sealed partial class TextEditor : UserControl, ISearchPanelHost, System.ComponentModel.INotifyPropertyChanged
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

    public static readonly DependencyProperty EditorFontFamilyProperty =
        DependencyProperty.Register(
            nameof(EditorFontFamily),
            typeof(FontFamily),
            typeof(TextEditor),
            new PropertyMetadata(EditorTextMetrics.CreateFontFamily(), OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontSizeProperty =
        DependencyProperty.Register(
            nameof(EditorFontSize),
            typeof(double),
            typeof(TextEditor),
            new PropertyMetadata(EditorTextMetrics.FontSize, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontWeightProperty =
        DependencyProperty.Register(
            nameof(EditorFontWeight),
            typeof(Windows.UI.Text.FontWeight),
            typeof(TextEditor),
            new PropertyMetadata(new Windows.UI.Text.FontWeight { Weight = 400 }, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontStyleProperty =
        DependencyProperty.Register(
            nameof(EditorFontStyle),
            typeof(Windows.UI.Text.FontStyle),
            typeof(TextEditor),
            new PropertyMetadata(Windows.UI.Text.FontStyle.Normal, OnEditorFontChanged));

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

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));

    private TextDocument? _attachedDocument;

#if WINDOWS_APP_SDK
    private TextEditorOptions? _currentOptions;
#endif

    public TextEditor()
    {
        this.InitializeComponent();
        PART_SearchPanel.Attach(this);
        PART_TextArea.CaretOffsetChanged  += OnTextAreaCaretOffsetChanged;
        PART_TextArea.SelectionChanged    += OnTextAreaSelectionChanged;
        PART_TextArea.NavigationRequested += (s, e) => NavigationRequested?.Invoke(this, e);
#if WINDOWS_APP_SDK
        PART_TextArea.PointerEntered += (s, e) => { PreviewMouseHover?.Invoke(this, EventArgs.Empty); MouseHover?.Invoke(this, EventArgs.Empty); };
        PART_TextArea.PointerExited  += (s, e) => { PreviewMouseHoverStopped?.Invoke(this, EventArgs.Empty); MouseHoverStopped?.Invoke(this, EventArgs.Empty); };
        Options.PropertyChanged += OnOptionsPropertyChanged;
        _currentOptions = Options;
#endif
        KeyDown += OnEditorKeyDown;
        ApplyThemeToChrome();
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

    // ── Public Properties ────────────────────────────────────────────────────

    [System.ComponentModel.Browsable(false)]
    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    [System.ComponentModel.Category("Selection")]
    [System.ComponentModel.Description("Caret position as a character offset into the document.")]
    public int CurrentOffset
    {
        get => (int)GetValue(CurrentOffsetProperty);
        set => SetValue(CurrentOffsetProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public int SelectionStartOffset
    {
        get => (int)GetValue(SelectionStartOffsetProperty);
        set => SetValue(SelectionStartOffsetProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public int SelectionEndOffset
    {
        get => (int)GetValue(SelectionEndOffsetProperty);
        set => SetValue(SelectionEndOffsetProperty, value);
    }

    [System.ComponentModel.Category("View")]
    [System.ComponentModel.Description("Colour theme applied to the editor chrome and line numbers.")]
    public TextEditorTheme Theme
    {
        get => (TextEditorTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public TextEditorOptions Options
    {
        get => (TextEditorOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    [System.ComponentModel.Category("Behavior")]
    [System.ComponentModel.Description("Prevents the user from modifying document content when true.")]
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    [System.ComponentModel.Category("Behavior")]
    [System.ComponentModel.Description("True when the document has changes since the last save.")]
    [System.ComponentModel.ReadOnly(true)]
    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    [System.ComponentModel.Category("View")]
    [System.ComponentModel.Description("Show or hide the line number gutter on the left margin.")]
    public bool ShowLineNumbers
    {
        get => (bool)GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    [System.ComponentModel.Category("View")]
    [System.ComponentModel.Description("Wrap long lines to fit within the visible width.")]
    public bool WordWrap
    {
        get => (bool)GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public Brush? LineNumbersForeground
    {
        get => (Brush?)GetValue(LineNumbersForegroundProperty);
        set => SetValue(LineNumbersForegroundProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.DisplayName("Editor Font Family")]
    [System.ComponentModel.Description("Font family used by the code text and line numbers.")]
    public FontFamily EditorFontFamily
    {
        get => (FontFamily)GetValue(EditorFontFamilyProperty);
        set => SetValue(EditorFontFamilyProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.DisplayName("Editor Font Size")]
    [System.ComponentModel.Description("Font size used by the code text and line numbers.")]
    public double EditorFontSize
    {
        get => (double)GetValue(EditorFontSizeProperty);
        set => SetValue(EditorFontSizeProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.DisplayName("Editor Font Weight")]
    [System.ComponentModel.Description("Font weight used by the code text and line numbers.")]
    public Windows.UI.Text.FontWeight EditorFontWeight
    {
        get => (Windows.UI.Text.FontWeight)GetValue(EditorFontWeightProperty);
        set => SetValue(EditorFontWeightProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.DisplayName("Editor Font Style")]
    [System.ComponentModel.Description("Font style used by the code text and line numbers.")]
    public Windows.UI.Text.FontStyle EditorFontStyle
    {
        get => (Windows.UI.Text.FontStyle)GetValue(EditorFontStyleProperty);
        set => SetValue(EditorFontStyleProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => (IHighlightingDefinition?)GetValue(SyntaxHighlightingProperty);
        set => SetValue(SyntaxHighlightingProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
    public Encoding Encoding
    {
        get => (Encoding)GetValue(EncodingProperty);
        set => SetValue(EncodingProperty, value);
    }

    [System.ComponentModel.Category("Scrolling")]
    [System.ComponentModel.Description("Visibility of the horizontal scroll bar.")]
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    [System.ComponentModel.Category("Scrolling")]
    [System.ComponentModel.Description("Visibility of the vertical scroll bar.")]
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    [System.ComponentModel.Browsable(false)]
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

    [System.ComponentModel.Browsable(false)]
    public int CaretOffset
    {
        get => CurrentOffset;
        set => CurrentOffset = value;
    }

    [System.ComponentModel.Category("Selection")]
    [System.ComponentModel.Description("Start offset of the current selection.")]
    public int SelectionStart
    {
        get => SelectionLength == 0 ? CurrentOffset : Math.Min(SelectionStartOffset, SelectionEndOffset);
        set => Select(value, SelectionLength);
    }

    [System.ComponentModel.Category("Selection")]
    [System.ComponentModel.Description("Length of the current selection in characters.")]
    public int SelectionLength
    {
        get => Math.Abs(SelectionEndOffset - SelectionStartOffset);
        set => Select(SelectionStart, value);
    }

    [System.ComponentModel.Browsable(false)]
    public string SelectedText
    {
        get
        {
            if (Document is null || SelectionLength == 0)
                return string.Empty;
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

    [System.ComponentModel.Category("Document")]
    [System.ComponentModel.Description("Number of lines in the document.")]
    [System.ComponentModel.ReadOnly(true)]
    public int LineCount => Document?.LineCount ?? 1;

    [System.ComponentModel.Category("Editing")]
    [System.ComponentModel.Description("True when there are changes that can be undone.")]
    [System.ComponentModel.ReadOnly(true)]
    public bool CanUndo => Document?.UndoStack.CanUndo ?? false;

    [System.ComponentModel.Category("Editing")]
    [System.ComponentModel.Description("True when there are changes that can be redone.")]
    [System.ComponentModel.ReadOnly(true)]
    public bool CanRedo => Document?.UndoStack.CanRedo ?? false;

    [System.ComponentModel.Category("View")]
    [System.ComponentModel.Description("True when the Find/Replace panel is currently visible.")]
    [System.ComponentModel.ReadOnly(true)]
    public bool IsSearchPanelOpen => PART_SearchPanel.IsOpen;

    [System.ComponentModel.Browsable(false)]
    public TextArea TextArea => PART_TextArea;

    [System.ComponentModel.Browsable(false)]
    public SearchPanel SearchPanel => PART_SearchPanel;

    [System.ComponentModel.Browsable(false)]
    public IReferenceSegmentSource? ReferenceSegmentSource
    {
        get => PART_TextArea.ReferenceSegmentSource;
        set => PART_TextArea.ReferenceSegmentSource = value;
    }

    [System.ComponentModel.Browsable(false)]
    public ICSharpCode.AvalonEdit.Folding.FoldingManager? FoldingManager
    {
        get => PART_TextArea.FoldingManager;
        set => PART_TextArea.FoldingManager = value;
    }

    [System.ComponentModel.Browsable(false)]
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

    // Mouse-hover events (WPF/AvalonEdit API compatibility)
    public event EventHandler? MouseHover;
    public event EventHandler? MouseHoverStopped;
    public event EventHandler? PreviewMouseHover;
    public event EventHandler? PreviewMouseHoverStopped;

#if !WINDOWS_APP_SDK
    // WPF-compat static sentinel fields referenced by some AvalonEdit callers.
    public static readonly object MouseHoverEvent = null;
    public static readonly object MouseHoverStoppedEvent = null;
    public static readonly object PreviewMouseHoverEvent = null;
    public static readonly object PreviewMouseHoverStoppedEvent = null;
#endif

    // ── Scroll metrics ───────────────────────────────────────────────────────

    public double HorizontalOffset => GetScrollViewerMetric("HorizontalOffset");
    public double VerticalOffset => GetScrollViewerMetric("VerticalOffset");
    public double ExtentHeight => GetScrollViewerMetric("ExtentHeight");
    public double ExtentWidth => GetScrollViewerMetric("ExtentWidth");
    public double ViewportHeight => GetScrollViewerMetric("ViewportHeight", ActualHeight);
    public double ViewportWidth => GetScrollViewerMetric("ViewportWidth", ActualWidth);

    // ── Operations ───────────────────────────────────────────────────────────

    /// <summary>Scroll the editor viewport so the given 1-based line number is visible.</summary>
    public void ScrollToLine(int lineNumber) => PART_TextArea.ScrollToLine(lineNumber);

    /// <summary>Scroll the editor viewport so the given offset is visible and move the caret there.</summary>
    public void ScrollToOffset(int offset)
    {
        if (Document is null) return;
        CurrentOffset = Math.Clamp(offset, 0, Document.TextLength);
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

    public void AppendText(string textData) => EnsureDocument().Insert(EnsureDocument().TextLength, textData ?? string.Empty);
    public void BeginChange()               => EnsureDocument().BeginUpdate();
    public IDisposable DeclareChangeBlock() => EnsureDocument().RunUpdate();
    public void EndChange()                 => EnsureDocument().EndUpdate();
    public void Clear()                     => Text = string.Empty;
    public void Undo() { if (Document?.UndoStack.CanUndo == true) Document.UndoStack.Undo(); }
    public void Redo() { if (Document?.UndoStack.CanRedo == true) Document.UndoStack.Redo(); }
    public void SelectAll() => Select(0, Document?.TextLength ?? 0);

    public void Load(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using var reader = new StreamReader(stream, Encoding ?? Encoding.UTF8, true, 1024, leaveOpen: true);
        Text = reader.ReadToEnd();
        Encoding = reader.CurrentEncoding;
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Load(string fileName)
    {
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        Load(fs);
    }

    public void Save(Stream stream)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using var writer = new StreamWriter(stream, Encoding ?? Encoding.UTF8, 1024, leaveOpen: true);
        if (Document is not null) Document.WriteTextTo(writer);
        else writer.Write(string.Empty);
        writer.Flush();
        Document?.UndoStack.MarkAsOriginalFile();
    }

    public void Save(string fileName)
    {
        if (fileName is null) throw new ArgumentNullException(nameof(fileName));
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        Save(fs);
    }

    public void OpenSearchPanel(string? initialText = null)
    {
#if WINDOWS_APP_SDK
        PART_SearchPanel.Open(initialText);
#else
        PART_SearchPanel.Open(initialText ?? GetSelectedTextOrNull());
#endif
    }

    public void CloseSearchPanel()   => PART_SearchPanel.Close();
    public void FindNext()           { if (!IsSearchPanelOpen) OpenSearchPanel(); PART_SearchPanel.FindNext(); }
    public void FindPrevious()       { if (!IsSearchPanelOpen) OpenSearchPanel(); PART_SearchPanel.FindPrevious(); }

    // Scroll helpers
    public void LineUp()    => ScrollCaretByLines(-1, false);
    public void LineDown()  => ScrollCaretByLines(1, false);
    public void LineLeft()  => ScrollHorizontalBy(-32);
    public void LineRight() => ScrollHorizontalBy(32);
    public void PageUp()    => ScrollCaretByLines(-GetApproxPageLineCount(), true);
    public void PageDown()  => ScrollCaretByLines(GetApproxPageLineCount(), true);
    public void PageLeft()  => ScrollHorizontalBy(-(ViewportWidth > 0 ? ViewportWidth : 240));
    public void PageRight() => ScrollHorizontalBy(ViewportWidth > 0 ? ViewportWidth : 240);
    public void ScrollTo(int line, int column) => ScrollToLine(line);
    public void ScrollToEnd()  { if (Document is null) return; CurrentOffset = Document.TextLength; ScrollToOffset(CurrentOffset); }
    public void ScrollToHome() { CurrentOffset = 0; ScrollToOffset(0); }
    public void ScrollToHorizontalOffset(double offset) => TryChangeView(offset, null, true);
    public void ScrollToVerticalOffset(double offset)   => TryChangeView(null, offset, true);

    public void Copy()
    {
        if (SelectionLength == 0) return;
        var package = new DataPackage();
        package.SetText(SelectedText);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }

    public void Cut()
    {
        if (IsReadOnly || SelectionLength == 0) return;
        Copy();
        DeleteSelection();
    }

    public void Delete()
    {
        if (IsReadOnly) return;
        if (SelectionLength > 0) { DeleteSelection(); return; }
        var doc = Document;
        if (doc is null) return;
        int offset = Math.Clamp(CurrentOffset, 0, doc.TextLength);
        if (offset < doc.TextLength) { doc.Remove(offset, 1); Select(offset, 0); }
    }

    public void Paste() => _ = PasteAsync();

    public TextViewPosition? GetPositionFromPoint(Windows.Foundation.Point point)
    {
        var document = Document;
        if (document is null) return null;
        var textView = GetInnerTextView();
        if (textView is null) return null;
        var method = textView.GetType().GetMethod("GetOffsetFromViewPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method is null) return null;
        var offsetObj = method.Invoke(textView, new object[] { point.X, point.Y });
        if (offsetObj is not int offset) return null;
        offset = Math.Clamp(offset, 0, document.TextLength);
        var location = document.GetLocation(offset);
        return new TextViewPosition(location.Line, location.Column);
    }

    public new void OnApplyTemplate() => base.OnApplyTemplate();

    // ── DP Callbacks ─────────────────────────────────────────────────────────

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
        editor.PART_TextArea.Document = args.NewValue as TextDocument;
        editor.PART_SearchPanel.UpdateDocument(args.NewValue as TextDocument);
#if !WINDOWS_APP_SDK
        editor.UpdateSummary();
#endif
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
        editor.NotifyPropertyChanged(nameof(Theme));
    }

    private static void OnOptionsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        var newOptions = (args.NewValue as TextEditorOptions) ?? new TextEditorOptions();
#if WINDOWS_APP_SDK
        if (editor._currentOptions is not null)
            editor._currentOptions.PropertyChanged -= editor.OnOptionsPropertyChanged;
        editor._currentOptions = newOptions;
        newOptions.PropertyChanged += editor.OnOptionsPropertyChanged;
#endif
        editor.PART_TextArea.Options = newOptions;
        editor.OptionChanged?.Invoke(editor, new PropertyChangedEventArgs(null));
    }

#if WINDOWS_APP_SDK
    private void OnOptionsPropertyChanged(object? sender, PropertyChangedEventArgs e) { }
#endif

    private static void OnIsReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.IsReadOnly = (bool)args.NewValue;
        editor.NotifyPropertyChanged(nameof(IsReadOnly));
    }

    private static void OnIsModifiedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        var document = editor.Document;
        if (document is null) return;
        if ((bool)args.NewValue)
        {
            if (document.UndoStack.IsOriginalFile) document.UndoStack.DiscardOriginalFileMarker();
        }
        else
        {
            document.UndoStack.MarkAsOriginalFile();
        }
    }

    private static void OnShowLineNumbersChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        bool show = (bool)args.NewValue;
        UnoEdit.Logging.HighlightLogger.Log("ShowLineNumbers", $"TextEditor.OnShowLineNumbersChanged: show={show}, PART_TextArea={editor.PART_TextArea?.GetType().Name ?? "NULL"}");
        if (editor.PART_TextArea is null) return;
        editor.PART_TextArea.ShowLineNumbers = show;
        editor.NotifyPropertyChanged(nameof(ShowLineNumbers));
    }

    private static void OnWordWrapChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.WordWrap = (bool)args.NewValue;
        editor.NotifyPropertyChanged(nameof(WordWrap));
    }

    private static void OnLineNumbersForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.LineNumbersForeground = args.NewValue as Brush;
#if !WINDOWS_APP_SDK
        if (args.NewValue is Brush brush)
            editor.SummaryTextBlock.Foreground = brush;
        else
            editor.ApplyThemeToChrome();
#endif
    }

    private static void OnEditorFontChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.EditorFontFamily = editor.EditorFontFamily;
        editor.PART_TextArea.EditorFontSize = editor.EditorFontSize;
        editor.PART_TextArea.EditorFontWeight = editor.EditorFontWeight;
        editor.PART_TextArea.EditorFontStyle = editor.EditorFontStyle;
        editor.NotifyPropertyChanged(nameof(EditorFontFamily));
        editor.NotifyPropertyChanged(nameof(EditorFontSize));
        editor.NotifyPropertyChanged(nameof(EditorFontWeight));
        editor.NotifyPropertyChanged(nameof(EditorFontStyle));
    }

    private static void OnSyntaxHighlightingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.SyntaxHighlighting = args.NewValue as IHighlightingDefinition;
    }

    private static void OnCurrentOffsetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        if (editor.PART_TextArea.CurrentOffset != (int)args.NewValue)
            editor.PART_TextArea.CurrentOffset = (int)args.NewValue;
#if !WINDOWS_APP_SDK
        editor.UpdateSummary();
#endif
    }

    private static void OnSelectionRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        if (editor.PART_TextArea.SelectionStartOffset != editor.SelectionStartOffset)
            editor.PART_TextArea.SelectionStartOffset = editor.SelectionStartOffset;
        if (editor.PART_TextArea.SelectionEndOffset != editor.SelectionEndOffset)
            editor.PART_TextArea.SelectionEndOffset = editor.SelectionEndOffset;
#if !WINDOWS_APP_SDK
        editor.UpdateSummary();
#endif
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        EditorBorder.Background  = new SolidColorBrush(t.EditorBackground);
        EditorBorder.BorderBrush = new SolidColorBrush(t.BorderColor);
#if !WINDOWS_APP_SDK
        TitleBarGrid.Background     = new SolidColorBrush(t.TitleBarBackground);
        TitleTextBlock.Foreground   = new SolidColorBrush(t.TitleBarForeground);
        SummaryTextBlock.Foreground = LineNumbersForeground ?? new SolidColorBrush(t.GutterForeground);
#endif
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
        TextChanged?.Invoke(this, EventArgs.Empty);
#if !WINDOWS_APP_SDK
        UpdateSummary();
#endif
    }

    private void OnUndoStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(ICSharpCode.AvalonEdit.Document.UndoStack.IsOriginalFile))
            SyncIsModifiedFromDocument();
    }

    private void OnTextAreaCaretOffsetChanged(object? sender, EventArgs e)
    {
        if (CurrentOffset != PART_TextArea.CurrentOffset)
            CurrentOffset = PART_TextArea.CurrentOffset;
#if !WINDOWS_APP_SDK
        UpdateSummary();
#endif
    }

    private void OnTextAreaSelectionChanged(object? sender, EventArgs e)
    {
        if (SelectionStartOffset != PART_TextArea.SelectionStartOffset)
            SelectionStartOffset = PART_TextArea.SelectionStartOffset;
        if (SelectionEndOffset != PART_TextArea.SelectionEndOffset)
            SelectionEndOffset = PART_TextArea.SelectionEndOffset;
#if !WINDOWS_APP_SDK
        UpdateSummary();
#endif
    }

#if !WINDOWS_APP_SDK
    internal void UpdateSummary()
    {
        if (Document is null)
        {
            SummaryTextBlock.Text = "No document";
            return;
        }
        int textLength = Document.TextLength;
        int currentOffset = Math.Clamp(CurrentOffset, 0, textLength);
        int selectionStart = Math.Clamp(SelectionStartOffset, 0, textLength);
        int selectionEnd   = Math.Clamp(SelectionEndOffset, 0, textLength);
        TextLocation location = Document.GetLocation(currentOffset);
        int selectionLength = Math.Abs(selectionEnd - selectionStart);
        string selectionSummary = selectionLength > 0 ? $"  Sel {selectionLength}" : string.Empty;
        SummaryTextBlock.Text = $"{Document.LineCount} lines  {Document.TextLength} chars  Ln {location.Line}, Col {location.Column}{selectionSummary}";
    }
#endif

    private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool controlPressed = IsControlPressed();
        bool shiftPressed = IsShiftPressed();

        if (controlPressed && e.Key == Windows.System.VirtualKey.F)
        {
            OpenSearchPanel(GetSelectedTextOrNull());
            e.Handled = true;
            return;
        }
        if (e.Key == Windows.System.VirtualKey.F3)
        {
            if (shiftPressed) FindPrevious(); else FindNext();
            e.Handled = true;
            return;
        }
        if (e.Key == Windows.System.VirtualKey.Escape && IsSearchPanelOpen)
        {
            CloseSearchPanel();
            e.Handled = true;
        }
    }

    private string? GetSelectedTextOrNull()
    {
        if (Document is null || SelectionStartOffset == SelectionEndOffset)
            return null;
        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset   = Math.Max(SelectionStartOffset, SelectionEndOffset);
        if (endOffset <= startOffset) return null;
        string text = Document.GetText(startOffset, endOffset - startOffset);
        return text.Contains('\n') ? null : text;
    }

    private static bool IsShiftPressed()
        => InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private static bool IsControlPressed()
    {
        var flags = Windows.UI.Core.CoreVirtualKeyStates.Down;
#if WINDOWS_APP_SDK
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftControl).HasFlag(flags);
#else
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftWindows).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightWindows).HasFlag(flags);
#endif
    }

    private TextDocument EnsureDocument()
    {
        if (Document is null) Document = new TextDocument();
        return Document;
    }

    private void SyncIsModifiedFromDocument()
    {
        bool isModified = Document is not null && !Document.UndoStack.IsOriginalFile;
        if (IsModified != isModified)
            SetValue(IsModifiedProperty, isModified);
    }

    private void DeleteSelection()
    {
        if (SelectionLength == 0 || Document is null) return;
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
        if (Document is null) return;
        var current = Document.GetLocation(Math.Clamp(CurrentOffset, 0, Document.TextLength));
        int targetLine = Math.Clamp(current.Line + lineDelta, 1, Document.LineCount);
        if (moveCaret) CurrentOffset = Document.GetOffset(targetLine, current.Column);
        ScrollToLine(targetLine);
    }

    private void ScrollHorizontalBy(double delta) => ScrollToHorizontalOffset(Math.Max(0, HorizontalOffset + delta));

    private async Task PasteAsync()
    {
        var view = Clipboard.GetContent();
        if (view is null || !view.Contains(StandardDataFormats.Text)) return;
        string text = await view.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;
        SelectedText = text;
        CurrentOffset = SelectionStart + text.Length;
        Select(CurrentOffset, 0);
    }

    private object? GetInnerTextView()
    {
        var property = PART_TextArea.GetType().GetProperty("PART_TextView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return property?.GetValue(PART_TextArea);
    }

    private object? GetInnerScrollViewer()
    {
        var textView = GetInnerTextView();
        if (textView is null) return null;
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
        method?.Invoke(scrollViewer, new object[] { horizontalOffset, verticalOffset, null, disableAnimation });
    }
}
