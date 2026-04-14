using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UnoEdit.Skia.Desktop.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace UnoEdit.WinUI.Controls;

/// <summary>
/// WinUI 3 TextEditor — thin wrapper around the ported UnoEdit rendering pipeline.
/// Delegates all editing and rendering to an inner <see cref="TextArea"/> and
/// <see cref="SearchPanel"/>.
/// </summary>
public sealed partial class TextEditor : UserControl, ISearchPanelHost
{
    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextEditor),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CurrentOffsetProperty =
        DependencyProperty.Register(nameof(CurrentOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0, OnCurrentOffsetChanged));

    public static readonly DependencyProperty SelectionStartOffsetProperty =
        DependencyProperty.Register(nameof(SelectionStartOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty SelectionEndOffsetProperty =
        DependencyProperty.Register(nameof(SelectionEndOffset), typeof(int), typeof(TextEditor),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(TextEditorTheme), typeof(TextEditor),
            new PropertyMetadata(TextEditorTheme.Dark, OnThemeChanged));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(nameof(Options), typeof(TextEditorOptions), typeof(TextEditor),
            new PropertyMetadata(new TextEditorOptions(), OnOptionsChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false, OnIsModifiedChanged));

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(true, OnShowLineNumbersChanged));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(TextEditor),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty SyntaxHighlightingProperty =
        DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition),
            typeof(TextEditor), new PropertyMetadata(null, OnSyntaxHighlightingChanged));

    public static readonly DependencyProperty EncodingProperty =
        DependencyProperty.Register(nameof(Encoding), typeof(Encoding), typeof(TextEditor),
            new PropertyMetadata(Encoding.UTF8));

    public static readonly DependencyProperty LineNumbersForegroundProperty =
        DependencyProperty.Register(nameof(LineNumbersForeground), typeof(Brush), typeof(TextEditor),
            new PropertyMetadata(null, OnLineNumbersForegroundChanged));

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(TextEditor),
            new PropertyMetadata(ScrollBarVisibility.Auto));

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

    // ── Public Properties ────────────────────────────────────────────────────

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

    public Brush? LineNumbersForeground
    {
        get => (Brush?)GetValue(LineNumbersForegroundProperty);
        set => SetValue(LineNumbersForegroundProperty, value);
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
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
            if (Document is null || SelectionLength == 0) return string.Empty;
            int start = Math.Min(SelectionStartOffset, SelectionEndOffset);
            return Document.GetText(start, SelectionLength);
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            var document = EnsureDocument();
            int start = SelectionStart;
            document.Replace(start, SelectionLength, value);
            Select(start, value.Length);
        }
    }

    public int LineCount => Document?.LineCount ?? 1;
    public bool CanUndo  => Document?.UndoStack.CanUndo ?? false;
    public bool CanRedo  => Document?.UndoStack.CanRedo ?? false;
    public bool IsSearchPanelOpen => PART_SearchPanel.IsOpen;

    /// <summary>Provides direct access to the inner <see cref="UnoEdit.Skia.Desktop.Controls.TextArea"/>.</summary>
    public TextArea TextArea => PART_TextArea;

    /// <summary>Provides direct access to the inner <see cref="UnoEdit.Skia.Desktop.Controls.SearchPanel"/>.</summary>
    public SearchPanel SearchPanel => PART_SearchPanel;

    public IReferenceSegmentSource? ReferenceSegmentSource
    {
        get => PART_TextArea.ReferenceSegmentSource;
        set => PART_TextArea.ReferenceSegmentSource = value;
    }

    public FoldingManager? FoldingManager
    {
        get => PART_TextArea.FoldingManager;
        set => PART_TextArea.FoldingManager = value;
    }

    public IHighlightedLineSource? HighlightedLineSource
    {
        get => PART_TextArea.HighlightedLineSource;
        set => PART_TextArea.HighlightedLineSource = value;
    }

    // Scroll metrics (delegated to inner ScrollViewer via reflection, matching Uno version)
    public double HorizontalOffset   => GetScrollViewerMetric("HorizontalOffset");
    public double VerticalOffset     => GetScrollViewerMetric("VerticalOffset");
    public double ExtentHeight       => GetScrollViewerMetric("ExtentHeight");
    public double ExtentWidth        => GetScrollViewerMetric("ExtentWidth");
    public double ViewportHeight     => GetScrollViewerMetric("ViewportHeight", ActualHeight);
    public double ViewportWidth      => GetScrollViewerMetric("ViewportWidth", ActualWidth);

    // ── Events ───────────────────────────────────────────────────────────────

    public event EventHandler? DocumentChanged;
    public event EventHandler? TextChanged;
    public event PropertyChangedEventHandler? OptionChanged;
    public event EventHandler<ReferenceSegment>? NavigationRequested;

    // Mouse-hover stubs (API compatibility)
    public event EventHandler? MouseHover;
    public event EventHandler? MouseHoverStopped;
    public event EventHandler? PreviewMouseHover;
    public event EventHandler? PreviewMouseHoverStopped;

    // ── Operations ───────────────────────────────────────────────────────────

    public void Select(int start, int length)
    {
        var document = Document;
        int docLen = document?.TextLength ?? 0;
        if (start < 0 || start > docLen) throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || start + length > docLen) throw new ArgumentOutOfRangeException(nameof(length));
        SetSelection(start, start + length);
    }

    public void SetSelection(int startOffset, int endOffset)
    {
        if (Document is null) return;
        int len = Document.TextLength;
        SelectionStartOffset = Math.Clamp(Math.Min(startOffset, endOffset), 0, len);
        SelectionEndOffset   = Math.Clamp(Math.Max(startOffset, endOffset), 0, len);
        CurrentOffset        = SelectionEndOffset;
    }

    public void ScrollToLine(int lineNumber)   => PART_TextArea.ScrollToLine(lineNumber);
    public void ScrollToOffset(int offset)
    {
        if (Document is null) return;
        CurrentOffset = Math.Clamp(offset, 0, Document.TextLength);
    }

    public void AppendText(string text) => EnsureDocument().Insert(EnsureDocument().TextLength, text ?? string.Empty);
    public void Clear() => Text = string.Empty;
    public void BeginChange()              => EnsureDocument().BeginUpdate();
    public IDisposable DeclareChangeBlock() => EnsureDocument().RunUpdate();
    public void EndChange()                => EnsureDocument().EndUpdate();
    public void Undo() { if (CanUndo) Document!.UndoStack.Undo(); }
    public void Redo() { if (CanRedo) Document!.UndoStack.Redo(); }
    public void SelectAll() => Select(0, Document?.TextLength ?? 0);

    public void Copy()
    {
        if (SelectionLength == 0) return;
        var pkg = new DataPackage();
        pkg.SetText(SelectedText);
        Clipboard.SetContent(pkg);
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

    // Search
    public void OpenSearchPanel(string? initialText = null) => PART_SearchPanel.Open(initialText);
    public void CloseSearchPanel()   => PART_SearchPanel.Close();
    public void FindNext()           { if (!IsSearchPanelOpen) PART_SearchPanel.Open(); PART_SearchPanel.FindNext(); }
    public void FindPrevious()       { if (!IsSearchPanelOpen) PART_SearchPanel.Open(); PART_SearchPanel.FindPrevious(); }

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
    public void ScrollToEnd()   { if (Document is null) return; CurrentOffset = Document.TextLength; ScrollToOffset(CurrentOffset); }
    public void ScrollToHome()  { CurrentOffset = 0; ScrollToOffset(0); }
    public void ScrollToHorizontalOffset(double offset) => TryChangeView(offset, null, true);
    public void ScrollToVerticalOffset(double offset)   => TryChangeView(null, offset, true);

    public TextViewPosition? GetPositionFromPoint(Point point)
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

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        editor.AttachDocument(e.OldValue as TextDocument, e.NewValue as TextDocument);
        editor.PART_TextArea.Document = e.NewValue as TextDocument;
        editor.PART_SearchPanel.UpdateDocument(e.NewValue as TextDocument);
        editor.DocumentChanged?.Invoke(editor, EventArgs.Empty);
        editor.TextChanged?.Invoke(editor, EventArgs.Empty);
    }

    private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        var theme = (e.NewValue as TextEditorTheme) ?? TextEditorTheme.Dark;
        editor.PART_TextArea.Theme = theme;
        editor.PART_SearchPanel.UpdateTheme(theme);
        editor.ApplyThemeToChrome();
    }

    private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        editor.PART_TextArea.Options = (e.NewValue as TextEditorOptions) ?? new TextEditorOptions();
        editor.OptionChanged?.Invoke(editor, new PropertyChangedEventArgs(null));
    }

    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).PART_TextArea.IsReadOnly = (bool)e.NewValue;

    private static void OnIsModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        var document = editor.Document;
        if (document is null) return;
        if ((bool)e.NewValue) { if (document.UndoStack.IsOriginalFile) document.UndoStack.DiscardOriginalFileMarker(); }
        else document.UndoStack.MarkAsOriginalFile();
    }

    private static void OnShowLineNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).PART_TextArea.ShowLineNumbers = (bool)e.NewValue;

    private static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).PART_TextArea.WordWrap = (bool)e.NewValue;

    private static void OnLineNumbersForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).PART_TextArea.LineNumbersForeground = e.NewValue as Brush;

    private static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TextEditor)d).PART_TextArea.SyntaxHighlighting = e.NewValue as IHighlightingDefinition;

    private static void OnCurrentOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (editor.PART_TextArea.CurrentOffset != (int)e.NewValue)
            editor.PART_TextArea.CurrentOffset = (int)e.NewValue;
    }

    private static void OnSelectionRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (TextEditor)d;
        if (editor.PART_TextArea.SelectionStartOffset != editor.SelectionStartOffset)
            editor.PART_TextArea.SelectionStartOffset = editor.SelectionStartOffset;
        if (editor.PART_TextArea.SelectionEndOffset != editor.SelectionEndOffset)
            editor.PART_TextArea.SelectionEndOffset = editor.SelectionEndOffset;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void AttachDocument(TextDocument? oldDoc, TextDocument? newDoc)
    {
        if (oldDoc is not null)
        {
            oldDoc.TextChanged -= OnDocumentTextChanged;
            oldDoc.UndoStack.PropertyChanged -= OnUndoStackPropertyChanged;
        }
        _attachedDocument = newDoc;
        if (newDoc is not null)
        {
            newDoc.TextChanged += OnDocumentTextChanged;
            newDoc.UndoStack.PropertyChanged += OnUndoStackPropertyChanged;
        }
        SyncIsModifiedFromDocument();
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        PART_SearchPanel.RefreshSearch();
        TextChanged?.Invoke(this, EventArgs.Empty);
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
    }

    private void OnTextAreaSelectionChanged(object? sender, EventArgs e)
    {
        if (SelectionStartOffset != PART_TextArea.SelectionStartOffset)
            SelectionStartOffset = PART_TextArea.SelectionStartOffset;
        if (SelectionEndOffset != PART_TextArea.SelectionEndOffset)
            SelectionEndOffset = PART_TextArea.SelectionEndOffset;
    }

    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        EditorBorder.Background  = new SolidColorBrush(t.EditorBackground);
        EditorBorder.BorderBrush = new SolidColorBrush(t.BorderColor);
    }

    private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool ctrl  = IsControlPressed();
        bool shift = IsShiftPressed();

        if (ctrl && e.Key == Windows.System.VirtualKey.F)
        {
            OpenSearchPanel(GetSelectedTextOrNull());
            e.Handled = true;
            return;
        }
        if (e.Key == Windows.System.VirtualKey.F3)
        {
            if (shift) FindPrevious(); else FindNext();
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
        if (Document is null || SelectionStartOffset == SelectionEndOffset) return null;
        int start = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int end   = Math.Max(SelectionStartOffset, SelectionEndOffset);
        if (end <= start) return null;
        string text = Document.GetText(start, end - start);
        return text.Contains('\n') ? null : text;
    }

    private void DeleteSelection()
    {
        if (SelectionLength == 0 || Document is null) return;
        int start = SelectionStart;
        Document.Remove(start, SelectionLength);
        Select(start, 0);
    }

    private TextDocument EnsureDocument()
    {
        if (Document is null) Document = new TextDocument();
        return Document;
    }

    private void SyncIsModifiedFromDocument()
    {
        bool isModified = Document is not null && !Document.UndoStack.IsOriginalFile;
        if (IsModified != isModified) SetValue(IsModifiedProperty, isModified);
    }

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

    private object? GetInnerTextView()
    {
        var prop = PART_TextArea.GetType().GetProperty("PART_TextView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return prop?.GetValue(PART_TextArea);
    }

    private object? GetInnerScrollViewer()
    {
        var tv = GetInnerTextView();
        if (tv is null) return null;
        var prop = tv.GetType().GetProperty("TextScrollViewer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return prop?.GetValue(tv);
    }

    private double GetScrollViewerMetric(string name, double fallback = 0)
    {
        var sv = GetInnerScrollViewer();
        var v  = sv?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(sv);
        return v is double d ? d : fallback;
    }

    private void TryChangeView(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
    {
        var sv = GetInnerScrollViewer();
        var m  = sv?.GetType().GetMethod("ChangeView", BindingFlags.Instance | BindingFlags.Public);
        m?.Invoke(sv, new object[] { horizontalOffset, verticalOffset, null, disableAnimation });
    }

    private static bool IsShiftPressed()
        => InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    private static bool IsControlPressed()
    {
        var flags = Windows.UI.Core.CoreVirtualKeyStates.Down;
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftControl).HasFlag(flags);
    }
}
