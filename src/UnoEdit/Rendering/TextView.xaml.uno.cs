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
            new PropertyMetadata(null, (d, _) =>
            {
                var tv = (TextView)d;
                tv._pendingFullRebuild = true;
                LogFlash("full queued: ReferenceSegmentSource changed");
                tv.RefreshViewport();
            }));

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
    private IHighlightedLineSource? _highlightedLineSource;
    private bool _highlightedLineSourceExplicitlySet;
    private int _firstVisibleLineNumber = 1;
    private int _lastVisibleLineNumber;
    private int _desiredColumn = 1;
    private int _selectionAnchorOffset;
    private bool _isPointerSelecting;
    // When > 0, RefreshViewport() is suppressed and a deferred refresh is pending.
    private int _suppressRefreshDepth;
    // Guards against re-entrant RefreshViewport() calls (e.g. from synchronous highlighting callbacks).
    private bool _isRefreshingViewport;
    // When true, the next RefreshViewport() must do a full rebuild (text/theme/fold changed).
    private bool _pendingFullRebuild = true;
    // Visible row range from the previous full RefreshViewport, used for partial-update detection.
    private int _prevFirstVisualRow = -1;
    private int _prevLastVisualRow = -1;
    // Theme at the last full rebuild — used to detect theme changes that invalidate cached Runs.
    private TextEditorTheme? _prevTheme;
    // Highlighter source at the last full rebuild — used to detect source swaps that invalidate cached Runs.
    private IHighlightedLineSource? _prevHighlightedLineSource;
    // Set when the current highlighter fires HighlightingInvalidated (e.g. theme change within the source).
    private bool _highlightingDataInvalidated;

    private static readonly bool s_debugFlash =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_FLASH"), "1", StringComparison.Ordinal);
    private static void LogFlash(string msg) { if (s_debugFlash) Console.WriteLine($"[Flash] {msg}"); }
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

    public IHighlightedLineSource? HighlightedLineSource
    {
        get => _highlightedLineSource;
        set
        {
            if (ReferenceEquals(_highlightedLineSource, value))
                return;

            if (_highlightedLineSource is not null)
            {
                _highlightedLineSource.HighlightingInvalidated -= OnHighlightedLineSourceInvalidated;
                _highlightedLineSource.SetDocument(null);
            }

            _highlightedLineSource = value;
            _highlightedLineSourceExplicitlySet = true;

            if (_highlightedLineSource is not null)
            {
                _highlightedLineSource.SetDocument(_document);
                _highlightedLineSource.HighlightingInvalidated += OnHighlightedLineSourceInvalidated;
            }

            _pendingFullRebuild = true;
            LogFlash("full queued: HighlightedLineSource changed");
            RefreshViewport();
        }
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
        textView._pendingFullRebuild = true;
        LogFlash("full queued: theme changed");
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
        tv._pendingFullRebuild = true;
        LogFlash("full queued: FoldingManager changed");
        tv.RefreshViewport();
    }

    private void OnFoldingsChanged(object? sender, EventArgs e)
    {
        RebuildVisibleLineList();
        _pendingFullRebuild = true;
        LogFlash("full queued: folds changed");
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
        _highlightedLineSource?.SetDocument(newDocument);

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
        _pendingFullRebuild = true;
        LogFlash("full queued: document attached");
        RefreshViewport();
    }

    private void OnHighlightedLineSourceInvalidated(object? sender, EventArgs e)
    {
        _pendingFullRebuild = true;
        _highlightingDataInvalidated = true;
        LogFlash("full queued: external highlighting invalidated");
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
        _pendingFullRebuild = true;
        LogFlash("full queued: document text changed");
        RefreshViewport();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTextMetrics();
        _pendingFullRebuild = true;
        LogFlash("full queued: OnLoaded");
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _pendingFullRebuild = true;
        LogFlash("full queued: OnSizeChanged");
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

    /// <summary>
    /// Suppresses <see cref="RefreshViewport"/> calls within the <paramref name="action"/> body
    /// and runs exactly one refresh afterward. Use when performing multiple document/selection
    /// changes that would otherwise each trigger a separate repaint (causing visible flash).
    /// </summary>
    internal void BatchRefresh(Action action)
    {
        _suppressRefreshDepth++;
        try
        {
            action();
        }
        finally
        {
            _suppressRefreshDepth--;
            if (_suppressRefreshDepth == 0)
            {
                RefreshViewport();
            }
        }
    }

    private void RefreshViewport()
    {
        if (_suppressRefreshDepth > 0)
        {
            LogFlash($"suppressed depth={_suppressRefreshDepth}");
            return;
        }

        if (_isRefreshingViewport)
        {
            // Re-entrant call from a synchronous highlighting callback (e.g. TMModel.ForceTokenization
            // firing ModelTokensChanged during HighlightLine).  _pendingFullRebuild is already set by
            // the caller; the outer RefreshViewport will finish the current frame correctly.
            return;
        }

        _isRefreshingViewport = true;
        try
        {
            RefreshViewportCore();
        }
        finally
        {
            _isRefreshingViewport = false;
        }
    }

    private void RefreshViewportCore()
    {

        if (_document is null)
        {
            _lines.Clear();
            TopSpacer.Height = 0;
            BottomSpacer.Height = 0;
            _firstVisibleLineNumber = 1;
            _lastVisibleLineNumber = 0;
            _prevFirstVisualRow = -1;
            _prevLastVisualRow = -1;
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

        int selectionStart = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int selectionEnd = Math.Max(SelectionStartOffset, SelectionEndOffset);
        bool hasSelection = selectionStart != selectionEnd;

        // ── Partial-update fast path ────────────────────────────────────────────
        // When only caret/selection positions changed (no text, fold, or theme
        // change, and the visible row window is unchanged), update only the
        // affected items in-place.  This avoids an ObservableCollection.Clear()
        // + sequential Add() that cause Skia to emit intermediate blank frames.
        bool canPartialUpdate =
            !_pendingFullRebuild
            && firstVisualRow == _prevFirstVisualRow
            && lastVisualRow  == _prevLastVisualRow
            && _lines.Count   == (lastVisualRow - firstVisualRow + 1);

        if (canPartialUpdate)
        {
            LogFlash($"partial rows={firstVisualRow}-{lastVisualRow} caret={CurrentOffset} sel={SelectionStartOffset}-{SelectionEndOffset}");
            for (int i = 0; i < _lines.Count; i++)
            {
                int vRow = firstVisualRow + i;
                int lineNumber = _visibleDocLines[vRow];
                DocumentLine line = _document.GetLineByNumber(lineNumber);
                string lineText = _document.GetText(line);

                bool isCaretLine = line.Offset <= CurrentOffset && CurrentOffset <= line.EndOffset;
                int caretColumn = isCaretLine ? _document.GetLocation(CurrentOffset).Column : 1;
                double caretLeft = Math.Max(0, GetDisplayColumnX(lineText, caretColumn - 1));

                double newSelectionOpacity = 0d;
                double newSelectionLeft = 0d;
                double newSelectionWidth = 0d;
                if (hasSelection)
                {
                    int lineSelStart = Math.Max(selectionStart, line.Offset);
                    int lineSelEnd   = Math.Min(selectionEnd,   line.EndOffset);
                    if (lineSelStart < lineSelEnd)
                    {
                        int startLogical = lineSelStart - line.Offset;
                        int endLogical   = lineSelEnd   - line.Offset;
                        newSelectionLeft  = Math.Max(0, GetDisplayColumnX(lineText, startLogical));
                        newSelectionWidth = Math.Max(2, GetDisplayColumnX(lineText, endLogical) - newSelectionLeft);
                        newSelectionOpacity = 0.45d;
                    }
                }

                double newCaretOpacity     = isCaretLine ? 1d    : 0d;
                double newHighlightOpacity = isCaretLine ? 0.18d : 0d;
                var    newCaretMargin      = new Thickness(caretLeft,        0, 0, 0);
                var    newSelectionMargin  = new Thickness(newSelectionLeft, 0, 0, 0);

                var oldVm = _lines[i];
                // Mutate the existing VM in-place — INPC setters fire only for changed values.
                oldVm.CaretOpacity     = newCaretOpacity;
                oldVm.HighlightOpacity = newHighlightOpacity;
                oldVm.CaretMargin      = newCaretMargin;
                oldVm.SelectionMargin  = newSelectionMargin;
                oldVm.SelectionWidth   = newSelectionWidth;
                oldVm.SelectionOpacity = newSelectionOpacity;
            }
            return;
        }

        // ── Full rebuild ────────────────────────────────────────────────────────
        bool themeChanged = !ReferenceEquals(Theme, _prevTheme);
        bool highlighterChanged = !ReferenceEquals(_highlightedLineSource, _prevHighlightedLineSource);
        bool highlightingInvalidated = _highlightingDataInvalidated;
        _pendingFullRebuild = false;
        _highlightingDataInvalidated = false;
        _prevFirstVisualRow = firstVisualRow;
        _prevLastVisualRow  = lastVisualRow;
        _prevTheme          = Theme;
        _prevHighlightedLineSource = _highlightedLineSource;

        int expectedCount = lastVisualRow - firstVisualRow + 1;
        LogFlash($"FULL rebuild rows={firstVisualRow}-{lastVisualRow} count={expectedCount} prevLineCount={_lines.Count} themeChanged={themeChanged}");

        // Build new view-models into a temporary list first so we can decide
        // whether to update in-place (avoids Clear() which flashes a blank frame)
        // or do a full Clear+Add (only needed when the row count changes).
        bool canReuseExisting = _lines.Count == expectedCount;
        var newVms = new TextLineViewModel[expectedCount];
        for (int i = 0; i < expectedCount; i++)
        {
            int vRow = firstVisualRow + i;
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

            string displayText = lineText.Length == 0 ? " " : lineText;
            FoldMarkerKind foldMarker = GetFoldMarkerKind(line);

            // If the line text and fold-marker haven't changed, reuse the existing VM's pre-computed
            // Runs (and ReferenceSegments) via WithCaretAndSelection.  HighlightedTextBlock will then
            // skip its Inlines rebuild because the Runs reference is identical, eliminating
            // the brief blank-text flash that otherwise occurs on every caret/selection change.
            if (canReuseExisting
                && !themeChanged
                && !highlighterChanged
                && !highlightingInvalidated
                && _lines[i].Text == displayText
                && _lines[i].FoldMarker == foldMarker
                && ReferenceSegmentSource is null)   // ref-segment source changes handled by _pendingFullRebuild
            {
                newVms[i] = _lines[i].WithCaretAndSelection(
                    isCaretLine ? 1d    : 0d,
                    isCaretLine ? 0.18d : 0d,
                    new Thickness(caretLeft,        0, 0, 0),
                    new Thickness(selectionLeft,    0, 0, 0),
                    selectionWidth,
                    selectionOpacity);
                continue;
            }

            HighlightedLine? highlightedLine = null;
            try {
                highlightedLine = _highlightedLineSource?.HighlightLine(lineNumber)
                    ?? (_highlightedLineSourceExplicitlySet ? null : _highlighter?.HighlightLine(lineNumber));
            } catch {
                /* ignore errors during highlighting */
            }

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

            newVms[i] = new TextLineViewModel(
                line.LineNumber,
                displayText,
                isCaretLine ? 1d : 0d,
                isCaretLine ? 0.18d : 0d,
                new Thickness(caretLeft, 0, 0, 0),
                new Thickness(selectionLeft, 0, 0, 0),
                selectionWidth,
                selectionOpacity,
                Theme,
                foldMarker,
                highlightedLine,
                lineRefs);
        }

        // Apply: mutate existing VMs in-place via UpdateFrom (raises only PropertyChanged
        // for properties that actually differ).  The ObservableCollection is never modified,
        // so ItemsControl keeps its visual tree intact → no flash.  Only fall back to
        // Clear+Add when the row count changes (first render / scroll).
        if (_lines.Count == expectedCount)
        {
            for (int i = 0; i < expectedCount; i++)
                _lines[i].UpdateFrom(newVms[i]);
            LogFlash($"  in-place UpdateFrom: {expectedCount} items");
        }
        else
        {
            _lines.Clear();
            foreach (var vm in newVms)
                _lines.Add(vm);
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
