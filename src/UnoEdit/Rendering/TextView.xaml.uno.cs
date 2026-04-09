using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Documents;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView : UserControl
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(
            nameof(Document),
            typeof(TextDocument),
            typeof(TextView),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CurrentOffsetProperty =
        DependencyProperty.Register(
            nameof(CurrentOffset),
            typeof(int),
            typeof(TextView),
            new PropertyMetadata(0, OnCurrentOffsetChanged));

    public static readonly DependencyProperty SelectionStartOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionStartOffset),
            typeof(int),
            typeof(TextView),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty SelectionEndOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionEndOffset),
            typeof(int),
            typeof(TextView),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(
            nameof(Theme),
            typeof(TextEditorTheme),
            typeof(TextView),
            new PropertyMetadata(TextEditorTheme.Dark, OnThemeChanged));

    public static readonly DependencyProperty ReferenceSegmentSourceProperty =
        DependencyProperty.Register(
            nameof(ReferenceSegmentSource),
            typeof(IReferenceSegmentSource),
            typeof(TextView),
            new PropertyMetadata(null, (d, _) => ((TextView)d).RefreshViewport()));

    public static readonly DependencyProperty FoldingManagerProperty =
        DependencyProperty.Register(
            nameof(FoldingManager),
            typeof(ICSharpCode.AvalonEdit.Folding.FoldingManager),
            typeof(TextView),
            new PropertyMetadata(null, OnFoldingManagerChanged));

    private readonly ObservableCollection<TextLineViewModel> _lines = new();
    private readonly TextBlock _measurementProbe = new()
    {
        TextWrapping = TextWrapping.NoWrap,
    };
    private const double LineHeight = 22d;
    private const double DefaultCharacterWidth = 7.8d;
    private const double TextLeftPadding = 0d;
    private const double GutterWidth = 72d;
    private const int OverscanLineCount = 4;
    private TextDocument? _document;
    private DocumentHighlighter? _highlighter;
    private int _firstVisibleLineNumber = 1;
    private int _lastVisibleLineNumber;
    private int _desiredColumn = 1;
    private int _selectionAnchorOffset;
    private bool _isPointerSelecting;
    private double _characterWidth = DefaultCharacterWidth;
    private List<int> _visibleDocLines = new();

    public event EventHandler? CaretOffsetChanged;
    public event EventHandler? SelectionChanged;

    private double CharacterWidth => _characterWidth;

    public TextView()
    {
        this.InitializeComponent();
        FontFamily = EditorTextMetrics.CreateFontFamily();
        FontSize = EditorTextMetrics.FontSize;
        _measurementProbe.FontFamily = EditorTextMetrics.CreateFontFamily();
        _measurementProbe.FontSize = EditorTextMetrics.FontSize;
        LinesItemsControl.ItemsSource = _lines;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        ApplyThemeToChrome();
        InitializePlatformInputBridge();
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

    public IReferenceSegmentSource? ReferenceSegmentSource
    {
        get => (IReferenceSegmentSource?)GetValue(ReferenceSegmentSourceProperty);
        set => SetValue(ReferenceSegmentSourceProperty, value);
    }

    public ICSharpCode.AvalonEdit.Folding.FoldingManager? FoldingManager
    {
        get => (ICSharpCode.AvalonEdit.Folding.FoldingManager?)GetValue(FoldingManagerProperty);
        set => SetValue(FoldingManagerProperty, value);
    }

    /// <summary>Raised when a reference segment is Ctrl+Clicked. The event arg carries the segment.</summary>
    public event EventHandler<ReferenceSegment>? NavigationRequested;

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
    }

    private static void OnCurrentOffsetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.HandleCurrentOffsetChanged((int)args.NewValue);
    }

    private static void OnSelectionRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.RefreshViewport();
        textView.SelectionChanged?.Invoke(textView, EventArgs.Empty);
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.ApplyThemeToChrome();
        textView.RefreshViewport();
    }

    private static void OnFoldingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        var tv = (TextView)d;
        if (args.OldValue is ICSharpCode.AvalonEdit.Folding.FoldingManager oldFm)
            oldFm.FoldingsChanged -= tv.OnFoldingsChanged;
        if (args.NewValue is ICSharpCode.AvalonEdit.Folding.FoldingManager newFm)
            newFm.FoldingsChanged += tv.OnFoldingsChanged;
        tv.RebuildVisibleLineList();
        tv.RefreshViewport();
    }

    private void OnFoldingsChanged(object? sender, EventArgs e)
    {
        RebuildVisibleLineList();
        RefreshViewport();
    }

    private void AttachDocument(TextDocument? oldDocument, TextDocument? newDocument)
    {
        if (oldDocument is not null)
        {
            oldDocument.TextChanged -= HandleDocumentTextChanged;
        }

        _document = newDocument;
        _highlighter = null;
        _visibleDocLines.Clear();

        if (newDocument is not null)
        {
            newDocument.TextChanged += HandleDocumentTextChanged;
            CurrentOffset = Math.Min(CurrentOffset, newDocument.TextLength);
            SelectionStartOffset = Math.Min(SelectionStartOffset, newDocument.TextLength);
            SelectionEndOffset = Math.Min(SelectionEndOffset, newDocument.TextLength);
            _selectionAnchorOffset = CurrentOffset;

            // Try to initialise highlighting for whichever language fits the document extension.
            // Default to C# if nothing else is detected.
            var definition = HighlightingManager.Instance.GetDefinition("C#")
                ?? (HighlightingManager.Instance.HighlightingDefinitions.Count > 0
                    ? HighlightingManager.Instance.HighlightingDefinitions[0]
                    : null);
            if (definition != null)
                _highlighter = new DocumentHighlighter(newDocument, definition);
        }
        else
        {
            CurrentOffset = 0;
            SelectionStartOffset = 0;
            SelectionEndOffset = 0;
            _selectionAnchorOffset = 0;
        }

        RebuildVisibleLineList();
        RefreshViewport();
    }

    /// <summary>Update named chrome elements in the XAML tree to match the current <see cref="Theme"/>.</summary>
    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        RootBorder.Background = new SolidColorBrush(t.EditorBackground);
    }

    private void HandleDocumentTextChanged(object? sender, EventArgs e)
    {
        if (_document is not null && CurrentOffset > _document.TextLength)
        {
            CurrentOffset = _document.TextLength;
        }

        if (_document is not null)
        {
            SelectionStartOffset = Math.Min(SelectionStartOffset, _document.TextLength);
            SelectionEndOffset = Math.Min(SelectionEndOffset, _document.TextLength);
        }

        RebuildVisibleLineList();
        RefreshViewport();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTextMetrics();
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    private void UpdateTextMetrics()
    {
        double measuredCharacterWidth = MeasureCharacterWidth();
        if (measuredCharacterWidth > 0)
        {
            _characterWidth = measuredCharacterWidth;
        }
    }

    private static double MeasureCharacterWidth()
    {
        const int sampleLength = 32;
        string sampleText = new('0', sampleLength);
        var probe = new TextBlock
        {
            Text = sampleText,
            FontFamily = EditorTextMetrics.CreateFontFamily(),
            FontSize = EditorTextMetrics.FontSize,
            TextWrapping = TextWrapping.NoWrap,
        };

        probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double width = probe.DesiredSize.Width;
        return width > 0 ? width / sampleLength : DefaultCharacterWidth;
    }

    private double GetDisplayColumnX(string lineText, int logicalColumn)
    {
        int clampedLogicalColumn = Math.Clamp(logicalColumn, 0, lineText.Length);
        if (clampedLogicalColumn == 0)
        {
            return 0d;
        }

        string prefix = TextLineViewModel.ExpandTabs(lineText[..clampedLogicalColumn]);
        return MeasureDisplayTextWidth(prefix);
    }

    private double MeasureDisplayTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0d;
        }

        _measurementProbe.Text = text;
        _measurementProbe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return _measurementProbe.DesiredSize.Width;
    }

    private int GetLogicalColumnFromDisplayX(string lineText, double documentX)
    {
        if (string.IsNullOrEmpty(lineText) || documentX <= 0)
        {
            return 0;
        }

        int low = 0;
        int high = lineText.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (GetDisplayColumnX(lineText, mid) <= documentX)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (low < lineText.Length)
        {
            double left = GetDisplayColumnX(lineText, low);
            double right = GetDisplayColumnX(lineText, low + 1);
            if (Math.Abs(documentX - right) < Math.Abs(documentX - left))
            {
                return low + 1;
            }
        }

        return low;
    }

    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    private void HandleCurrentOffsetChanged(int offset)
    {
        if (_document is null)
        {
            return;
        }

        if (offset < 0 || offset > _document.TextLength)
        {
            CurrentOffset = Math.Clamp(offset, 0, _document.TextLength);
            return;
        }

        _desiredColumn = _document.GetLocation(CurrentOffset).Column;
        EnsureCaretVisible();
        RefreshViewport();
        UpdatePlatformInputBridge();
        CaretOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Scroll the viewport so a 1-based line number is visible, without moving the caret.</summary>
    public void ScrollToLine(int lineNumber)
    {
        if (_document is null) return;
        int clamped = Math.Clamp(lineNumber, 1, _document.LineCount);
        int visualRow = GetVisualRow(clamped);
        if (visualRow < 0) return;
        double targetTop = visualRow * LineHeight;
        double viewportTop    = TextScrollViewer.VerticalOffset;
        double viewportBottom = viewportTop + TextScrollViewer.ViewportHeight;
        if (targetTop < viewportTop || targetTop + LineHeight > viewportBottom)
            TextScrollViewer.ChangeView(null, targetTop, null, false);
    }

    private void EnsureCaretVisible()
    {
        if (_document is null)
        {
            return;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        int visualRow = GetVisualRow(location.Line);
        if (visualRow < 0) return; // caret on a hidden line (shouldn't normally occur)
        double targetTop = Math.Max(0, visualRow * LineHeight);
        double targetBottom = targetTop + LineHeight;
        double viewportTop = TextScrollViewer.VerticalOffset;
        double viewportBottom = viewportTop + TextScrollViewer.ViewportHeight;

        if (targetTop < viewportTop)
        {
            TextScrollViewer.ChangeView(null, targetTop, null, true);
        }
        else if (targetBottom > viewportBottom && TextScrollViewer.ViewportHeight > 0)
        {
            TextScrollViewer.ChangeView(null, targetBottom - TextScrollViewer.ViewportHeight, null, true);
        }
    }

    private void RefreshViewport()
    {
        if (_document is null)
        {
            _lines.Clear();
            TopSpacer.Height = 0;
            BottomSpacer.Height = 0;
            _firstVisibleLineNumber = 1;
            _lastVisibleLineNumber = 0;
            return;
        }

        // Ensure visible-line list is populated (it may be empty on first call).
        if (_visibleDocLines.Count == 0 && _document.LineCount > 0)
            RebuildVisibleLineList();

        int totalVisualRows = _visibleDocLines.Count;
        double verticalOffset = TextScrollViewer.VerticalOffset;
        double viewportHeight = TextScrollViewer.ViewportHeight;
        if (viewportHeight <= 0)
        {
            viewportHeight = ActualHeight > 0 ? ActualHeight : 400;
        }

        int visibleRowCount = Math.Max(1, (int)Math.Ceiling(viewportHeight / LineHeight) + (OverscanLineCount * 2));
        int firstVisualRow = Math.Max(0, ((int)(verticalOffset / LineHeight)) - OverscanLineCount);
        int lastVisualRow = Math.Min(totalVisualRows - 1, firstVisualRow + visibleRowCount - 1);

        if (totalVisualRows > 0)
        {
            _firstVisibleLineNumber = _visibleDocLines[firstVisualRow];
            _lastVisibleLineNumber  = _visibleDocLines[lastVisualRow];
        }

        _lines.Clear();
        int selectionStart = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int selectionEnd = Math.Max(SelectionStartOffset, SelectionEndOffset);
        bool hasSelection = selectionStart != selectionEnd;

        for (int vRow = firstVisualRow; vRow <= lastVisualRow; vRow++)
        {
            int lineNumber = _visibleDocLines[vRow];
            DocumentLine line = _document.GetLineByNumber(lineNumber);
            string lineText = _document.GetText(line);
            bool isCaretLine = line.Offset <= CurrentOffset && CurrentOffset <= line.EndOffset;
            int caretColumn = isCaretLine ? _document.GetLocation(CurrentOffset).Column : 1;
            // Convert logical column to visual column so tab characters are handled correctly.
            double caretLeft = Math.Max(0, GetDisplayColumnX(lineText, caretColumn - 1));
            double selectionOpacity = 0d;
            double selectionLeft = 0d;
            double selectionWidth = 0d;

            if (hasSelection)
            {
                int lineSelectionStart = Math.Max(selectionStart, line.Offset);
                int lineSelectionEnd = Math.Min(selectionEnd, line.EndOffset);
                if (lineSelectionStart < lineSelectionEnd)
                {
                    int startLogical = lineSelectionStart - line.Offset;
                    int endLogical   = lineSelectionEnd   - line.Offset;
                    double startX = GetDisplayColumnX(lineText, startLogical);
                    double endX = GetDisplayColumnX(lineText, endLogical);
                    selectionLeft = Math.Max(0, startX);
                    selectionWidth = Math.Max(2, endX - startX);
                    selectionOpacity = 0.45d;
                }
            }

            HighlightedLine? highlightedLine = null;
            try { highlightedLine = _highlighter?.HighlightLine(lineNumber); } catch { /* ignore errors during highlighting */ }

            // Collect reference segments for this line, converted to line-relative visual-column offsets
            // so HighlightedTextBlock can compare them against visual run positions directly.
            System.Collections.Generic.IReadOnlyList<ReferenceSegment>? lineRefs = null;
            if (ReferenceSegmentSource is { } refSrc)
            {
                var segs = refSrc.GetSegments(line.Offset, line.EndOffset);
                if (segs.Count > 0)
                {
                    var converted = new System.Collections.Generic.List<ReferenceSegment>(segs.Count);
                    foreach (var seg in segs)
                    {
                        // Clip to line boundaries, then convert to visual column range.
                        int logStart = Math.Max(0, seg.StartOffset - line.Offset);
                        int logEnd   = Math.Min(lineText.Length, seg.EndOffset - line.Offset);
                        int visStart = TextLineViewModel.LogicalToVisualColumn(lineText, logStart);
                        int visEnd   = TextLineViewModel.LogicalToVisualColumn(lineText, logEnd);
                        converted.Add(new ReferenceSegment
                        {
                            StartOffset = visStart,
                            EndOffset   = visEnd,
                            Reference   = seg.Reference,
                            IsLocal     = seg.IsLocal,
                        });
                    }
                    lineRefs = converted;
                }
            }

            FoldMarkerKind foldMarker = GetFoldMarkerKind(line);

            _lines.Add(new TextLineViewModel(
                line.LineNumber,
                lineText.Length == 0 ? " " : lineText,
                isCaretLine ? 1d : 0d,
                isCaretLine ? 0.18d : 0d,
                new Thickness(caretLeft, 0, 0, 0),
                new Thickness(selectionLeft, 0, 0, 0),
                selectionWidth,
                selectionOpacity,
                Theme,
                foldMarker,
                highlightedLine,
                lineRefs));
        }

        TopSpacer.Height = firstVisualRow * LineHeight;
        BottomSpacer.Height = Math.Max(0, (totalVisualRows - 1 - lastVisualRow) * LineHeight);
    }

    private int ClampLineNumber(int lineNumber)
    {
        return _document is null ? 1 : Math.Clamp(lineNumber, 1, _document.LineCount);
    }

    private static int ClampColumn(DocumentLine line, int column)
    {
        return Math.Clamp(column, 1, line.Length + 1);
    }

    /// <summary>Rebuild <see cref="_visibleDocLines"/> from the current document and folding state.</summary>
    private void RebuildVisibleLineList()
    {
        _visibleDocLines.Clear();
        if (_document is null) return;
        for (int ln = 1; ln <= _document.LineCount; ln++)
        {
            if (!IsLineHidden(ln))
                _visibleDocLines.Add(ln);
        }
    }

    /// <summary>Return true if a document line number is hidden inside a folded section.</summary>
    private bool IsLineHidden(int lineNumber)
    {
        var fm = FoldingManager;
        if (fm is null || _document is null) return false;
        foreach (var section in fm.AllFoldings)
        {
            if (!section.IsFolded) continue;
            int foldStartLine = _document.GetLineByOffset(section.StartOffset).LineNumber;
            int foldEndLine   = _document.GetLineByOffset(section.EndOffset).LineNumber;
            if (lineNumber > foldStartLine && lineNumber <= foldEndLine)
                return true;
        }
        return false;
    }

    /// <summary>Return the 0-based visual row index for a document line number, or -1 if hidden.</summary>
    private int GetVisualRow(int docLineNumber)
    {
        if (_visibleDocLines.Count == 0) return 0;
        int idx = _visibleDocLines.BinarySearch(docLineNumber);
        return idx; // negative means hidden
    }

    /// <summary>Determine the fold-marker kind for a document line (whether it starts a fold).</summary>
    private FoldMarkerKind GetFoldMarkerKind(DocumentLine line)
    {
        var fm = FoldingManager;
        if (fm is null) return FoldMarkerKind.None;
        foreach (var section in fm.AllFoldings)
        {
            if (section.StartOffset >= line.Offset && section.StartOffset <= line.EndOffset)
                return section.IsFolded ? FoldMarkerKind.CanExpand : FoldMarkerKind.CanFold;
        }
        return FoldMarkerKind.None;
    }

    /// <summary>Toggle the first fold on the caret line. Returns true if a fold was toggled.</summary>
    private int GetOffsetFromViewPoint(double x, double y)
    {
        if (_document is null)
        {
            return 0;
        }

        double absoluteY = y + TextScrollViewer.VerticalOffset;
        int visualRow = Math.Clamp((int)(absoluteY / LineHeight), 0, _visibleDocLines.Count - 1);
        int targetLine = _visibleDocLines.Count > 0 ? _visibleDocLines[visualRow] : 1;
        DocumentLine documentLine = _document.GetLineByNumber(targetLine);

        double documentX = x + TextScrollViewer.HorizontalOffset - GutterWidth - TextLeftPadding;
        string lineText = _document.GetText(documentLine);
        int logicalColumn = GetLogicalColumnFromDisplayX(lineText, documentX);
        int targetColumn = Math.Clamp(logicalColumn + 1, 1, documentLine.Length + 1);
        _desiredColumn = targetColumn;
        return _document.GetOffset(targetLine, targetColumn);
    }

    partial void InitializePlatformInputBridge();
    partial void UpdatePlatformInputBridge();
    partial void FocusPlatformInputBridge();
    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed);

}
