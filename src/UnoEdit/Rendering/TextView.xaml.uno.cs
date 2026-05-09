using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel.Design;
using System.Windows.Documents;
using System.Windows.Input;
using UnoEdit.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView : UserControl, ICaretAnchorProvider, ITextView, IInlineObjectHost
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

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options),
            typeof(TextEditorOptions),
            typeof(TextView),
            new PropertyMetadata(new TextEditorOptions(), OnOptionsChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(TextView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowLineNumbers),
            typeof(bool),
            typeof(TextView),
            new PropertyMetadata(true, OnShowLineNumbersChanged));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(
            nameof(WordWrap),
            typeof(bool),
            typeof(TextView),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty IndentationStrategyProperty =
        DependencyProperty.Register(
            nameof(IndentationStrategy),
            typeof(IIndentationStrategy),
            typeof(TextView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty OverstrikeModeProperty =
        DependencyProperty.Register(
            nameof(OverstrikeMode),
            typeof(bool),
            typeof(TextView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty LineNumbersForegroundProperty =
        DependencyProperty.Register(
            nameof(LineNumbersForeground),
            typeof(Brush),
            typeof(TextView),
            new PropertyMetadata(null, OnLineNumbersForegroundChanged));

    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(TextView),
            new PropertyMetadata(null, OnSelectionStyleChanged));

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register(
            nameof(SelectionForeground),
            typeof(Brush),
            typeof(TextView),
            new PropertyMetadata(null, OnSelectionStyleChanged));

    public static readonly DependencyProperty SelectionBorderProperty =
        DependencyProperty.Register(
            nameof(SelectionBorder),
            typeof(Brush),
            typeof(TextView),
            new PropertyMetadata(null, OnSelectionStyleChanged));

    public static readonly DependencyProperty SelectionCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(SelectionCornerRadius),
            typeof(double),
            typeof(TextView),
            new PropertyMetadata(0d, OnSelectionStyleChanged));

    public static readonly DependencyProperty SyntaxHighlightingProperty =
        DependencyProperty.Register(
            nameof(SyntaxHighlighting),
            typeof(IHighlightingDefinition),
            typeof(TextView),
            new PropertyMetadata(null, OnSyntaxHighlightingChanged));

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

    public static readonly DependencyProperty EditorFontFamilyProperty =
        DependencyProperty.Register(
            nameof(EditorFontFamily),
            typeof(FontFamily),
            typeof(TextView),
            new PropertyMetadata(EditorTextMetrics.CreateFontFamily(), OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontSizeProperty =
        DependencyProperty.Register(
            nameof(EditorFontSize),
            typeof(double),
            typeof(TextView),
            new PropertyMetadata(EditorTextMetrics.FontSize, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontWeightProperty =
        DependencyProperty.Register(
            nameof(EditorFontWeight),
            typeof(Windows.UI.Text.FontWeight),
            typeof(TextView),
            new PropertyMetadata(new Windows.UI.Text.FontWeight { Weight = 400 }, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontStyleProperty =
        DependencyProperty.Register(
            nameof(EditorFontStyle),
            typeof(Windows.UI.Text.FontStyle),
            typeof(TextView),
            new PropertyMetadata(Windows.UI.Text.FontStyle.Normal, OnEditorFontChanged));

    private readonly ObservableCollection<TextLineViewModel> _lines = new();
    private readonly ServiceContainer _services = new();
    private readonly TextBlock _measurementProbe = new()
    {
        TextWrapping = TextWrapping.NoWrap,
    };
    private const double LineHeight = 22d;
    private const double DefaultCharacterWidth = 7.8d;
    private const double TextLeftPadding = 0d;
    private const double GutterWidth = 56d; // 40 (line numbers) + 16 (fold marker)
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
    private FontFamily? _prevEditorFontFamily;
    private double _prevEditorFontSize = double.NaN;
    private Windows.UI.Text.FontWeight _prevEditorFontWeight;
    private Windows.UI.Text.FontStyle _prevEditorFontStyle = Windows.UI.Text.FontStyle.Normal;
    // Highlighter source at the last full rebuild — used to detect source swaps that invalidate cached Runs.
    private IHighlightedLineSource? _prevHighlightedLineSource;
    // Set when the current highlighter fires HighlightingInvalidated (e.g. theme change within the source).
    private bool _highlightingDataInvalidated;
    private bool _caretVisible;
    private readonly HashSet<int> _dirtyHighlightedLines = new();
    private bool _highlightRangeRefreshQueued;
    private bool _awaitingHighlightedLineSourceReady;

    private static void LogFlash(string msg) { HighlightLogger.Log("Flash", msg); }
    private static void LogRender(string msg) { HighlightLogger.Log("Render", msg); }
    private static void LogPerf(string msg) { HighlightLogger.Log("Perf", msg); }
    private double _characterWidth = DefaultCharacterWidth;
    private List<int> _visibleDocLines = new();
    private double _lastPublishedHorizontalOffset;
    private double _lastPublishedVerticalOffset;
    private bool _visibleLinesPublished;

    public event EventHandler? CaretOffsetChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? VisibleLinesChanged;
    public event EventHandler? ScrollOffsetChanged;
    public int FirstVisibleLineNumber => _firstVisibleLineNumber;
    public int LastVisibleLineNumber => _lastVisibleLineNumber;

    private double CharacterWidth => _characterWidth;

    public TextView()
    {
        this.InitializeComponent();
        _services.AddService(typeof(TextView), this);
        ApplyEditorFont();
        LineNumberItemsControl.ItemsSource = _lines;
        FoldMarginItemsControl.ItemsSource = _lines;
        LinesItemsControl.ItemsSource = _lines;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        GotFocus += OnTextViewGotFocus;
        LostFocus += OnTextViewLostFocus;
        ApplyThemeToChrome();
        InitializePlatformInputBridge();
    }

    // Inline object host implementation registered into Document.ServiceProvider
    public void AttachInlineElement(UIElement element)
    {
        if (element == null) return;
        if (InlineObjectsCanvas == null) return;
        if (!InlineObjectsCanvas.Children.Contains(element))
            InlineObjectsCanvas.Children.Add(element);
    }

    public void DetachInlineElement(UIElement element)
    {
        if (element == null) return;
        if (InlineObjectsCanvas == null) return;
        InlineObjectsCanvas.Children.Remove(element);
    }

    public InlineElementMetrics MeasureInlineElement(UIElement element)
    {
        if (element == null)
            return new InlineElementMetrics(new Size(0, 0), 0);

        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return new InlineElementMetrics(element.DesiredSize, element.DesiredSize.Height);
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

    public IIndentationStrategy? IndentationStrategy
    {
        get => (IIndentationStrategy?)GetValue(IndentationStrategyProperty);
        set => SetValue(IndentationStrategyProperty, value);
    }

    public bool OverstrikeMode
    {
        get => (bool)GetValue(OverstrikeModeProperty);
        set => SetValue(OverstrikeModeProperty, value);
    }

    public Brush? LineNumbersForeground
    {
        get => (Brush?)GetValue(LineNumbersForegroundProperty);
        set => SetValue(LineNumbersForegroundProperty, value);
    }

    public Brush? SelectionBrush
    {
        get => (Brush?)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public Brush? SelectionForeground
    {
        get => (Brush?)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
    }

    public Brush? SelectionBorder
    {
        get => (Brush?)GetValue(SelectionBorderProperty);
        set => SetValue(SelectionBorderProperty, value);
    }

    public double SelectionCornerRadius
    {
        get => (double)GetValue(SelectionCornerRadiusProperty);
        set => SetValue(SelectionCornerRadiusProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.Description("Font family used by editor text and line numbers.")]
    public FontFamily EditorFontFamily
    {
        get => (FontFamily)GetValue(EditorFontFamilyProperty);
        set => SetValue(EditorFontFamilyProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.Description("Font size used by editor text and line numbers.")]
    public double EditorFontSize
    {
        get => (double)GetValue(EditorFontSizeProperty);
        set => SetValue(EditorFontSizeProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.Description("Font weight used by editor text and line numbers.")]
    public Windows.UI.Text.FontWeight EditorFontWeight
    {
        get => (Windows.UI.Text.FontWeight)GetValue(EditorFontWeightProperty);
        set => SetValue(EditorFontWeightProperty, value);
    }

    [System.ComponentModel.Category("Appearance")]
    [System.ComponentModel.Description("Font style used by editor text and line numbers.")]
    public Windows.UI.Text.FontStyle EditorFontStyle
    {
        get => (Windows.UI.Text.FontStyle)GetValue(EditorFontStyleProperty);
        set => SetValue(EditorFontStyleProperty, value);
    }

    public IReadOnlySectionProvider? ReadOnlySectionProvider { get; set; }

    public IServiceContainer Services => _services;

    internal event EventHandler<TextEventArgs>? TextCopied;
    internal event EventHandler<TextCompositionEventArgs>? TextEntering;
    internal event EventHandler<TextCompositionEventArgs>? TextEntered;

    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => (IHighlightingDefinition?)GetValue(SyntaxHighlightingProperty);
        set => SetValue(SyntaxHighlightingProperty, value);
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
                if (_highlightedLineSource is IRangeInvalidatingHighlightedLineSource oldRangeSource)
                {
                    oldRangeSource.HighlightingRangeInvalidated -= OnHighlightedLineSourceRangeInvalidated;
                }
                if (_highlightedLineSource is ITextViewAwareHighlightedLineSource oldAwareSource)
                {
                    oldAwareSource.SetTextView(null);
                }
                _highlightedLineSource.SetDocument(null);
            }

            _highlightedLineSource = value;
            _highlightedLineSourceExplicitlySet = true;
            _awaitingHighlightedLineSourceReady = false;

            if (_highlightedLineSource is not null)
            {
                if (_highlightedLineSource is ITextViewAwareHighlightedLineSource awareSource)
                {
                    awareSource.SetTextView(this);
                }
                _highlightedLineSource.SetDocument(_document);
                _highlightedLineSource.HighlightingInvalidated += OnHighlightedLineSourceInvalidated;
                if (_highlightedLineSource is IRangeInvalidatingHighlightedLineSource rangeSource)
                {
                    rangeSource.HighlightingRangeInvalidated += OnHighlightedLineSourceRangeInvalidated;
                }
            }

            _pendingFullRebuild = true;
            _highlightingDataInvalidated = true;
            _dirtyHighlightedLines.Clear();

            bool deferInitialRefresh =
                _highlightedLineSource is IVisibleRangeWarmableHighlightedLineSource
                && _highlightedLineSource is IRangeInvalidatingHighlightedLineSource;

            if (deferInitialRefresh)
            {
                _awaitingHighlightedLineSourceReady = true;
                WarmHighlightedLineSourceVisibleRange();
                LogFlash("full queued: HighlightedLineSource changed (awaiting ready)");
                return;
            }

            LogFlash("full queued: HighlightedLineSource changed");
            RefreshViewport();
        }
    }

    /// <summary>Raised when a reference segment is Ctrl+Clicked. The event arg carries the segment.</summary>
    public event EventHandler<ReferenceSegment>? NavigationRequested;

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        object? instance = _services.GetService(serviceType);
        if (instance is not null)
        {
            return instance;
        }

        return _document?.ServiceProvider.GetService(serviceType);
    }

    public bool TryGetCaretAnchorRect(out Rect rect)
    {
        rect = CalculatePlatformInputCaretRect();
        return !rect.IsEmpty;
    }

    internal void SetCaretVisible(bool isVisible)
    {
        if (_caretVisible == isVisible)
        {
            return;
        }

        _caretVisible = isVisible;
        RefreshViewport();
    }

    private void OnTextViewGotFocus(object sender, RoutedEventArgs e)
    {
        SetCaretVisible(true);
    }

    private void OnTextViewLostFocus(object sender, RoutedEventArgs e)
    {
        SetCaretVisible(false);
    }

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
        textView.UpdatePlatformInputBridge();
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

    private static void OnOptionsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView._pendingFullRebuild = true;
        LogFlash("full queued: options changed");
        textView.RefreshViewport();
    }

    private static void OnShowLineNumbersChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        bool show = (bool)args.NewValue;
        UnoEdit.Logging.HighlightLogger.Log("ShowLineNumbers", $"TextView.OnShowLineNumbersChanged: show={show}, LineNumberItemsControl={textView.LineNumberItemsControl?.GetType().Name ?? "NULL"}");
        // Mirror AvalonEdit: only the LineNumberMargin is added/removed; FoldingMargin stays.
        // Set column width explicitly in addition to Visibility so the Auto column remeasures
        // reliably on the Uno Skia renderer when re-enabling line numbers.
        if (textView.LineNumberItemsControl is null) return;
        textView.LineNumberItemsControl.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        textView.LineNumberColumn.Width = show ? GridLength.Auto : new GridLength(0);
        UnoEdit.Logging.HighlightLogger.Log("ShowLineNumbers", $"TextView.OnShowLineNumbersChanged done: Visibility={textView.LineNumberItemsControl.Visibility}, ColumnWidth={textView.LineNumberColumn.Width}");
    }

    private static void OnWordWrapChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.ApplyWordWrap();
        textView._pendingFullRebuild = true;
        LogFlash("full queued: word wrap changed");
        textView.RefreshViewport();
    }

    private static void OnLineNumbersForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView._pendingFullRebuild = true;
        LogFlash("full queued: line numbers foreground changed");
        textView.RefreshViewport();
    }

    private static void OnSelectionStyleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView._pendingFullRebuild = true;
        LogFlash("full queued: selection style changed");
        textView.RefreshViewport();
    }

    private static void OnSyntaxHighlightingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.UpdateDocumentHighlighter();
        textView._pendingFullRebuild = true;
        LogFlash("full queued: syntax highlighting changed");
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

    private static void OnEditorFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        var tv = (TextView)d;
        tv.ApplyEditorFont();
        tv.UpdateTextMetrics();
        tv._pendingFullRebuild = true;
        LogFlash("full queued: editor font changed");
        tv.RefreshViewport();
    }

    private void ApplyEditorFont()
    {
        FontFamily = EditorFontFamily;
        FontSize = EditorFontSize;
        FontWeight = EditorFontWeight;
        FontStyle = EditorFontStyle;
        _measurementProbe.FontFamily = EditorFontFamily;
        _measurementProbe.FontSize = EditorFontSize;
        _measurementProbe.FontWeight = EditorFontWeight;
        _measurementProbe.FontStyle = EditorFontStyle;
    }

    private bool FontSettingsEqual()
    {
        return _prevEditorFontFamily is not null
            && string.Equals(_prevEditorFontFamily.Source, EditorFontFamily.Source, StringComparison.Ordinal)
            && Math.Abs(_prevEditorFontSize - EditorFontSize) < 0.001
            && _prevEditorFontWeight.Weight == EditorFontWeight.Weight
            && _prevEditorFontStyle == EditorFontStyle;
    }

    private void StoreCurrentFontSettings()
    {
        _prevEditorFontFamily = EditorFontFamily;
        _prevEditorFontSize = EditorFontSize;
        _prevEditorFontWeight = EditorFontWeight;
        _prevEditorFontStyle = EditorFontStyle;
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
            // Remove any previously-registered inline-host service
            if (oldDocument.ServiceProvider is ServiceContainer oldContainer) {
                oldContainer.RemoveService(typeof(IInlineObjectHost));
            }
        }

        _document = newDocument;
        _visibleDocLines.Clear();
        _highlightedLineSource?.SetDocument(newDocument);

        if (newDocument is not null)
        {
            // Register this control as the inline-object host on the document's service provider so
            // shared rendering code can attach/detach inline UIElements via IInlineObjectHost.
            try {
                var container = newDocument.ServiceProvider as ServiceContainer;
                if (container == null) {
                    container = new ServiceContainer();
                    newDocument.ServiceProvider = container;
                }
                container.AddService(typeof(IInlineObjectHost), this);
            } catch {
                // swallow if service registration fails for any reason
            }
            newDocument.TextChanged += HandleDocumentTextChanged;
            CurrentOffset = Math.Min(CurrentOffset, newDocument.TextLength);
            SelectionStartOffset = Math.Min(SelectionStartOffset, newDocument.TextLength);
            SelectionEndOffset = Math.Min(SelectionEndOffset, newDocument.TextLength);
            _selectionAnchorOffset = CurrentOffset;
        }
        else
        {
            CurrentOffset = 0;
            SelectionStartOffset = 0;
            SelectionEndOffset = 0;
            _selectionAnchorOffset = 0;
        }

        UpdateDocumentHighlighter();
        RebuildVisibleLineList();
        _pendingFullRebuild = true;
        LogFlash("full queued: document attached");
        RefreshViewport();
    }

    private void UpdateDocumentHighlighter()
    {
        _highlighter = null;

        if (_document is null)
        {
            return;
        }

        IHighlightingDefinition? definition = SyntaxHighlighting;
        if (definition is null && !_highlightedLineSourceExplicitlySet)
        {
            definition = HighlightingManager.Instance.GetDefinition("C#")
                ?? (HighlightingManager.Instance.HighlightingDefinitions.Count > 0
                    ? HighlightingManager.Instance.HighlightingDefinitions[0]
                    : null);
        }

        if (definition is not null)
        {
            _highlighter = new DocumentHighlighter(_document, definition);
        }
    }

    private void WarmHighlightedLineSourceVisibleRange()
    {
        if (_highlightedLineSource is not IVisibleRangeWarmableHighlightedLineSource warmableSource)
        {
            return;
        }

        int startLineNumber = FirstVisibleLineNumber;
        int endLineNumber = LastVisibleLineNumber;
        if (startLineNumber <= 0 || endLineNumber <= 0)
        {
            if (!TryGetCurrentVisibleRowWindow(out int firstVisualRow, out int lastVisualRow)
                || _visibleDocLines.Count == 0)
            {
                return;
            }

            startLineNumber = _visibleDocLines[firstVisualRow];
            endLineNumber = _visibleDocLines[lastVisualRow];
        }

        if (startLineNumber <= 0 || endLineNumber < startLineNumber)
        {
            return;
        }

        LogRender($"warm-highlight visibleLines={startLineNumber}-{endLineNumber}");
        warmableSource.WarmVisibleLineRange(startLineNumber, endLineNumber);
    }

    private bool TryGetVisibleLineNumberRange(out int startLineNumber, out int endLineNumber)
    {
        startLineNumber = FirstVisibleLineNumber;
        endLineNumber = LastVisibleLineNumber;
        if (startLineNumber > 0 && endLineNumber >= startLineNumber)
        {
            return true;
        }

        if (!TryGetCurrentVisibleRowWindow(out int firstVisualRow, out int lastVisualRow)
            || _visibleDocLines.Count == 0)
        {
            startLineNumber = 0;
            endLineNumber = 0;
            return false;
        }

        startLineNumber = _visibleDocLines[firstVisualRow];
        endLineNumber = _visibleDocLines[lastVisualRow];
        return startLineNumber > 0 && endLineNumber >= startLineNumber;
    }

    private bool IsHighlightedLineSourceVisibleRangeReady()
    {
        if (_highlightedLineSource is not IVisibleRangeReadyHighlightedLineSource readySource)
        {
            return true;
        }

        if (!TryGetVisibleLineNumberRange(out int startLineNumber, out int endLineNumber))
        {
            return false;
        }

        return readySource.IsVisibleLineRangeReady(startLineNumber, endLineNumber);
    }

    private void OnHighlightedLineSourceInvalidated(object? sender, EventArgs e)
    {
        _pendingFullRebuild = true;
        _highlightingDataInvalidated = true;
        _dirtyHighlightedLines.Clear();
        _awaitingHighlightedLineSourceReady = false;
        LogFlash("full queued: external highlighting invalidated");
        RefreshViewport();
    }

    private void OnHighlightedLineSourceRangeInvalidated(object? sender, HighlightedLineRangeInvalidatedEventArgs e)
    {
        if (_awaitingHighlightedLineSourceReady && IsHighlightedLineSourceVisibleRangeReady())
        {
            _pendingFullRebuild = true;
            _awaitingHighlightedLineSourceReady = false;
        }

        _highlightingDataInvalidated = true;
        for (int lineNumber = e.StartLineNumber; lineNumber <= e.EndLineNumber; lineNumber++)
        {
            _dirtyHighlightedLines.Add(lineNumber);
        }

        LogFlash($"range queued: external highlighting invalidated lines={e.StartLineNumber}-{e.EndLineNumber}");
        QueueHighlightedRangeRefresh();
    }

    private void QueueHighlightedRangeRefresh()
    {
        if (_highlightRangeRefreshQueued)
        {
            return;
        }

        _highlightRangeRefreshQueued = true;

        void RefreshQueuedRange()
        {
            _highlightRangeRefreshQueued = false;
            RefreshViewport();
        }

        var dispatcherQueue = DispatcherQueue;
        if (dispatcherQueue is null || !dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, RefreshQueuedRange))
        {
            RefreshQueuedRange();
        }
    }

    /// <summary>Update named chrome elements in the XAML tree to match the current <see cref="Theme"/>.</summary>
    private void ApplyThemeToChrome()
    {
        var t = Theme ?? TextEditorTheme.Dark;
        RootBorder.Background = new SolidColorBrush(t.EditorBackground);
    }

    private void ApplyWordWrap()
    {
        TextScrollViewer.HorizontalScrollBarVisibility = WordWrap
            ? ScrollBarVisibility.Disabled
            : ScrollBarVisibility.Auto;
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
        ApplyWordWrap();
        _pendingFullRebuild = true;
        LogFlash("full queued: OnLoaded");
        RefreshViewport();
        UpdatePlatformInputBridge();
        PublishScrollOffset();
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

    private double MeasureCharacterWidth()
    {
        const int sampleLength = 32;
        string sampleText = new('0', sampleLength);
        var probe = new TextBlock
        {
            Text = sampleText,
            FontFamily = EditorFontFamily,
            FontSize = EditorFontSize,
            FontWeight = EditorFontWeight,
            FontStyle = EditorFontStyle,
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
        bool hasWindow = TryGetCurrentVisibleRowWindow(out int firstVisualRow, out int lastVisualRow);
        int rowDelta = hasWindow && _prevFirstVisualRow >= 0
            ? firstVisualRow - _prevFirstVisualRow
            : 0;
        LogRender($"view-changed intermediate={e.IsIntermediate} h={TextScrollViewer.HorizontalOffset:0.###} v={TextScrollViewer.VerticalOffset:0.###} viewportH={TextScrollViewer.ViewportHeight:0.###} lines={_lines.Count} window={(hasWindow ? $"{firstVisualRow}-{lastVisualRow}" : "n/a")} delta={rowDelta}");
        PublishScrollOffset();
        if (CanSkipScrollRefresh())
        {
            LogPerf($"scroll-skip intermediate={e.IsIntermediate} window={(hasWindow ? $"{firstVisualRow}-{lastVisualRow}" : "n/a")} delta={rowDelta}");
            UpdatePlatformInputBridge();
            return;
        }

        var sw = Stopwatch.StartNew();
        RefreshViewport(e.IsIntermediate ? "scroll-intermediate" : "scroll-final");
        sw.Stop();
        LogPerf($"scroll-refresh intermediate={e.IsIntermediate} window={(hasWindow ? $"{firstVisualRow}-{lastVisualRow}" : "n/a")} delta={rowDelta} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
        UpdatePlatformInputBridge();
    }

    private bool CanSkipScrollRefresh()
    {
        if (_document is null || _pendingFullRebuild || _highlightingDataInvalidated || _dirtyHighlightedLines.Count > 0)
        {
            return false;
        }

        if (!TryGetCurrentVisibleRowWindow(out int firstVisualRow, out int lastVisualRow))
        {
            return false;
        }

        return firstVisualRow == _prevFirstVisualRow && lastVisualRow == _prevLastVisualRow;
    }

    private bool TryGetCurrentVisibleRowWindow(out int firstVisualRow, out int lastVisualRow)
    {
        firstVisualRow = -1;
        lastVisualRow = -1;

        int totalVisualRows = _visibleDocLines.Count;
        if (totalVisualRows <= 0)
        {
            return false;
        }

        double verticalOffset = TextScrollViewer.VerticalOffset;
        double viewportHeight = TextScrollViewer.ViewportHeight;
        if (viewportHeight <= 0)
        {
            viewportHeight = ActualHeight > 0 ? ActualHeight : 400;
        }

        int visibleRowCount = Math.Max(1, (int)Math.Ceiling(viewportHeight / LineHeight) + (OverscanLineCount * 2));
        firstVisualRow = Math.Max(0, ((int)(verticalOffset / LineHeight)) - OverscanLineCount);
        lastVisualRow = Math.Min(totalVisualRows - 1, firstVisualRow + visibleRowCount - 1);
        return lastVisualRow >= firstVisualRow;
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

    private void RefreshViewport(string reason = "unspecified")
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

        var sw = Stopwatch.StartNew();
        bool reran = false;
        _isRefreshingViewport = true;
        try
        {
            RefreshViewportCore(reason);
            // A re-entrant ModelTokensChanged callback (fired synchronously by ForceTokenization
            // inside HighlightLine) may have set _pendingFullRebuild while we were executing the
            // line loop above.  Run one additional pass — still inside the re-entrant guard — to
            // pick up the fresh tokenization results without scheduling a separate UI cycle.
            if (_pendingFullRebuild)
            {
                reran = true;
                LogFlash("re-run: re-entrant highlight invalidation pending");
                RefreshViewportCore(reason + "+rerun");
            }
        }
        finally
        {
            _isRefreshingViewport = false;
            sw.Stop();
            LogPerf($"refresh reason={reason} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###} rerun={reran} lineVmCount={_lines.Count}");
        }
    }

    private void RefreshViewportCore(string reason)
    {
        var sw = Stopwatch.StartNew();

        if (_document is null)
        {
            _lines.Clear();
            TopSpacer.Height = 0;
            BottomSpacer.Height = 0;
            _firstVisibleLineNumber = 1;
            _lastVisibleLineNumber = 0;
            _prevFirstVisualRow = -1;
            _prevLastVisualRow = -1;
            PublishVisibleLinesState(0, -1);
            sw.Stop();
            LogPerf($"refresh-core reason={reason} path=empty elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
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
        LogRender($"viewport rows={firstVisualRow}-{lastVisualRow} totalRows={totalVisualRows} visibleLines={_firstVisibleLineNumber}-{_lastVisibleLineNumber} linesCount={_lines.Count} pendingFull={_pendingFullRebuild}");
        PublishVisibleLinesState(firstVisualRow, lastVisualRow);

        int selectionStart = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int selectionEnd = Math.Max(SelectionStartOffset, SelectionEndOffset);
        bool hasSelection = selectionStart != selectionEnd;
        bool themeChanged = !ReferenceEquals(Theme, _prevTheme);
        bool editorFontChanged = !FontSettingsEqual();
        bool highlighterChanged = !ReferenceEquals(_highlightedLineSource, _prevHighlightedLineSource);
        bool highlightingInvalidated = _highlightingDataInvalidated;
        HashSet<int>? dirtyHighlightedLines = _dirtyHighlightedLines.Count > 0
            ? new HashSet<int>(_dirtyHighlightedLines)
            : null;
        int expectedCount = lastVisualRow - firstVisualRow + 1;

        if (_awaitingHighlightedLineSourceReady && !IsHighlightedLineSourceVisibleRangeReady())
        {
            LogFlash($"defer full rebuild: highlighted source not ready lines={_firstVisibleLineNumber}-{_lastVisibleLineNumber}");
            QueueHighlightedRangeRefresh();
            sw.Stop();
            LogPerf($"refresh-core reason={reason} path=deferred-highlight rows={firstVisualRow}-{lastVisualRow} expectedCount={expectedCount} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
            return;
        }

        // ── Partial-update fast path ────────────────────────────────────────────
        // When only caret/selection positions changed (no text, fold, or theme
        // change, and the visible row window is unchanged), update only the
        // affected items in-place.  This avoids an ObservableCollection.Clear()
        // + sequential Add() that cause Skia to emit intermediate blank frames.
        bool canPartialUpdate =
            !_pendingFullRebuild
            && !themeChanged
            && !editorFontChanged
            && !highlighterChanged
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
                GetPreeditVisualRange(line, lineText, out int preeditVisualStart, out int preeditVisualEnd);
                GetPreeditUnderlineLayout(line, lineText, out double preeditUnderlineLeft, out double preeditUnderlineWidth, out double preeditUnderlineOpacity);

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

                double newCaretOpacity     = isCaretLine && _caretVisible ? 1d : 0d;
                double newHighlightOpacity = isCaretLine ? 0.18d : 0d;
                var    newCaretMargin      = new Thickness(caretLeft,        0, 0, 0);
                var    newSelectionMargin  = new Thickness(newSelectionLeft, 0, 0, 0);

                var oldVm = _lines[i];
                bool requiresHighlightRefresh = highlightingInvalidated
                    && dirtyHighlightedLines?.Contains(lineNumber) == true;
                if (!requiresHighlightRefresh)
                {
                    // Mutate the existing VM in-place — INPC setters fire only for changed values.
                    oldVm.CaretOpacity     = newCaretOpacity;
                    oldVm.HighlightOpacity = newHighlightOpacity;
                    oldVm.CaretMargin      = newCaretMargin;
                    oldVm.SelectionMargin  = newSelectionMargin;
                    oldVm.SelectionWidth   = newSelectionWidth;
                    oldVm.SelectionOpacity = newSelectionOpacity;
                    oldVm.PreeditUnderlineMargin = new Thickness(preeditUnderlineLeft, 0, 0, 0);
                    oldVm.PreeditUnderlineWidth = preeditUnderlineWidth;
                    oldVm.PreeditUnderlineOpacity = preeditUnderlineOpacity;
                    oldVm.PreeditVisualStart = preeditVisualStart;
                    oldVm.PreeditVisualEnd = preeditVisualEnd;
                    continue;
                }

                string displayText = lineText.Length == 0 ? " " : lineText;
                FoldMarkerKind foldMarker = GetFoldMarkerKind(line);

                HighlightedLine? highlightedLine = null;
                try
                {
                    highlightedLine = _highlightedLineSource?.HighlightLine(lineNumber)
                        ?? (_highlightedLineSourceExplicitlySet ? null : _highlighter?.HighlightLine(lineNumber));
                }
                catch
                {
                    /* ignore errors during highlighting */
                }
                LogFlash($"partial line={lineNumber} highlightedLine={(highlightedLine == null ? "null" : $"{highlightedLine.Sections.Count} sections")}");

                System.Collections.Generic.IReadOnlyList<ReferenceSegment>? lineRefs = null;
                if (ReferenceSegmentSource is { } refSrc)
                {
                    var segs = refSrc.GetSegments(line.Offset, line.EndOffset);
                    if (segs.Count > 0)
                    {
                        var converted = new System.Collections.Generic.List<ReferenceSegment>(segs.Count);
                        foreach (var seg in segs)
                        {
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

                oldVm.UpdateFrom(new TextLineViewModel(
                    line.LineNumber,
                    displayText,
                    newCaretOpacity,
                    newHighlightOpacity,
                    newCaretMargin,
                    newSelectionMargin,
                    newSelectionWidth,
                    newSelectionOpacity,
                    new Thickness(preeditUnderlineLeft, 0, 0, 0),
                    preeditUnderlineWidth,
                    preeditUnderlineOpacity,
                    Theme,
                    WordWrap,
                    (LineNumbersForeground as SolidColorBrush)?.Color,
                    (SelectionBrush as SolidColorBrush)?.Color,
                    (SelectionBorder as SolidColorBrush)?.Color,
                    SelectionCornerRadius,
                    foldMarker,
                    highlightedLine,
                    lineRefs,
                    preeditVisualStart,
                    preeditVisualEnd));
            }
            _highlightingDataInvalidated = false;
            _dirtyHighlightedLines.Clear();
            sw.Stop();
            LogPerf($"refresh-core reason={reason} path=partial rows={firstVisualRow}-{lastVisualRow} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
            return;
        }

        bool canReuseExisting = _lines.Count == expectedCount;
        Dictionary<int, (TextLineViewModel ViewModel, int Index)>? reusableViewModelsByLineNumber = null;
        if (canReuseExisting && !themeChanged && !editorFontChanged && !highlighterChanged && ReferenceSegmentSource is null)
        {
            reusableViewModelsByLineNumber = new Dictionary<int, (TextLineViewModel ViewModel, int Index)>(_lines.Count);
            for (int existingIndex = 0; existingIndex < _lines.Count; existingIndex++)
            {
                var existingVm = _lines[existingIndex];
                if (int.TryParse(existingVm.LineNumber, out int existingLineNumber))
                {
                    reusableViewModelsByLineNumber[existingLineNumber] = (existingVm, existingIndex);
                }
            }
        }

        int rowDelta = _prevFirstVisualRow >= 0 ? firstVisualRow - _prevFirstVisualRow : 0;
        bool canApplyScrollShift =
            !_pendingFullRebuild
            && !themeChanged
            && !editorFontChanged
            && !highlighterChanged
            && !highlightingInvalidated
            && dirtyHighlightedLines is null
            && canReuseExisting
            && ReferenceSegmentSource is null
            && reason.StartsWith("scroll", StringComparison.Ordinal)
            && rowDelta != 0
            && Math.Abs(rowDelta) < expectedCount;

        if (canApplyScrollShift)
        {
            LogFlash($"SHIFT rows={firstVisualRow}-{lastVisualRow} delta={rowDelta} count={expectedCount}");
            if (rowDelta > 0)
            {
                for (int shift = 0; shift < rowDelta; shift++)
                {
                    var recycled = _lines[0];
                    _lines.RemoveAt(0);
                    int targetVisualRow = lastVisualRow - rowDelta + 1 + shift;
                    recycled.UpdateFrom(BuildLineViewModel(targetVisualRow, expectedCount - rowDelta + shift, selectionStart, selectionEnd, hasSelection));
                    _lines.Add(recycled);
                }
            }
            else
            {
                int shiftCount = -rowDelta;
                for (int shift = 0; shift < shiftCount; shift++)
                {
                    var recycled = _lines[_lines.Count - 1];
                    _lines.RemoveAt(_lines.Count - 1);
                    int targetVisualRow = firstVisualRow + (shiftCount - 1 - shift);
                    recycled.UpdateFrom(BuildLineViewModel(targetVisualRow, shiftCount - 1 - shift, selectionStart, selectionEnd, hasSelection));
                    _lines.Insert(0, recycled);
                }
            }

            _pendingFullRebuild = false;
            _highlightingDataInvalidated = false;
            _dirtyHighlightedLines.Clear();
            _prevFirstVisualRow = firstVisualRow;
            _prevLastVisualRow = lastVisualRow;
            _prevTheme = Theme;
            StoreCurrentFontSettings();
            _prevHighlightedLineSource = _highlightedLineSource;
            TopSpacer.Height = firstVisualRow * LineHeight;
            BottomSpacer.Height = Math.Max(0, (totalVisualRows - 1 - lastVisualRow) * LineHeight);
            LogVisibleLineNumbers("shift", _lines);
            sw.Stop();
            LogPerf($"refresh-core reason={reason} path=shift rows={firstVisualRow}-{lastVisualRow} delta={rowDelta} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
            return;
        }

        // ── Full rebuild ────────────────────────────────────────────────────────
        _pendingFullRebuild = false;
        _highlightingDataInvalidated = false;
        _dirtyHighlightedLines.Clear();

        LogFlash($"FULL rebuild rows={firstVisualRow}-{lastVisualRow} count={expectedCount} prevLineCount={_lines.Count} themeChanged={themeChanged} editorFontChanged={editorFontChanged}");

        // Build new view-models into a temporary list first so we can decide
        // whether to update in-place (avoids Clear() which flashes a blank frame)
        // or do a full Clear+Add (only needed when the row count changes).
        var newVms = new TextLineViewModel[expectedCount];
        for (int i = 0; i < expectedCount; i++)
        {
            newVms[i] = BuildLineViewModel(firstVisualRow + i, i, selectionStart, selectionEnd, hasSelection);
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
            LogVisibleLineNumbers("in-place", newVms);
        }
        else
        {
            _lines.Clear();
            foreach (var vm in newVms)
                _lines.Add(vm);
            LogVisibleLineNumbers("replace", newVms);
        }

        _prevFirstVisualRow = firstVisualRow;
        _prevLastVisualRow  = lastVisualRow;
        _prevTheme          = Theme;
        StoreCurrentFontSettings();
        _prevHighlightedLineSource = _highlightedLineSource;
        TopSpacer.Height = firstVisualRow * LineHeight;
        BottomSpacer.Height = Math.Max(0, (totalVisualRows - 1 - lastVisualRow) * LineHeight);
        sw.Stop();
        LogPerf($"refresh-core reason={reason} path=full rows={firstVisualRow}-{lastVisualRow} expectedCount={expectedCount} elapsedMs={sw.Elapsed.TotalMilliseconds:0.###}");
    }

    private TextLineViewModel BuildLineViewModel(int visualRow, int targetIndex, int selectionStart, int selectionEnd, bool hasSelection)
    {
        int lineNumber = _visibleDocLines[visualRow];
        DocumentLine line = _document!.GetLineByNumber(lineNumber);
        string lineText = _document.GetText(line);
        bool isCaretLine = line.Offset <= CurrentOffset && CurrentOffset <= line.EndOffset;
        int caretColumn = isCaretLine ? _document.GetLocation(CurrentOffset).Column : 1;
        double caretLeft = Math.Max(0, GetDisplayColumnX(lineText, caretColumn - 1));
        GetPreeditVisualRange(line, lineText, out int preeditVisualStart, out int preeditVisualEnd);
        GetPreeditUnderlineLayout(line, lineText, out double preeditUnderlineLeft, out double preeditUnderlineWidth, out double preeditUnderlineOpacity);
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
                int endLogical = lineSelectionEnd - line.Offset;
                double startX = GetDisplayColumnX(lineText, startLogical);
                double endX = GetDisplayColumnX(lineText, endLogical);
                selectionLeft = Math.Max(0, startX);
                selectionWidth = Math.Max(2, endX - startX);
                selectionOpacity = 0.45d;
            }
        }

        string displayText = lineText.Length == 0 ? " " : lineText;
        FoldMarkerKind foldMarker = GetFoldMarkerKind(line);

        if (!(_highlightingDataInvalidated || _dirtyHighlightedLines.Count > 0)
            && ReferenceSegmentSource is null
            && ReferenceEquals(_highlightedLineSource, _prevHighlightedLineSource))
        {
            for (int existingIndex = 0; existingIndex < _lines.Count; existingIndex++)
            {
                var existingVm = _lines[existingIndex];
                if (int.TryParse(existingVm.LineNumber, out int existingLineNumber)
                    && existingLineNumber == lineNumber
                    && existingVm.Text == displayText
                    && existingVm.FoldMarker == foldMarker)
                {
                    return existingVm.WithCaretAndSelection(
                        isCaretLine && _caretVisible ? 1d : 0d,
                        isCaretLine ? 0.18d : 0d,
                        new Thickness(caretLeft, 0, 0, 0),
                        new Thickness(selectionLeft, 0, 0, 0),
                        selectionWidth,
                        selectionOpacity,
                        new Thickness(preeditUnderlineLeft, 0, 0, 0),
                        preeditUnderlineWidth,
                        preeditUnderlineOpacity,
                        preeditVisualStart,
                        preeditVisualEnd,
                        forceClone: existingIndex != targetIndex);
                }
            }
        }

        HighlightedLine? highlightedLine = null;
        try
        {
            highlightedLine = _highlightedLineSource?.HighlightLine(lineNumber)
                ?? (_highlightedLineSourceExplicitlySet ? null : _highlighter?.HighlightLine(lineNumber));
        }
        catch
        {
            /* ignore errors during highlighting */
        }
        LogFlash($"line={lineNumber} highlightedLine={(highlightedLine == null ? "null" : $"{highlightedLine.Sections.Count} sections")}");

        System.Collections.Generic.IReadOnlyList<ReferenceSegment>? lineRefs = null;
        if (ReferenceSegmentSource is { } refSrc)
        {
            var segs = refSrc.GetSegments(line.Offset, line.EndOffset);
            if (segs.Count > 0)
            {
                var converted = new System.Collections.Generic.List<ReferenceSegment>(segs.Count);
                foreach (var seg in segs)
                {
                    int logStart = Math.Max(0, seg.StartOffset - line.Offset);
                    int logEnd = Math.Min(lineText.Length, seg.EndOffset - line.Offset);
                    int visStart = TextLineViewModel.LogicalToVisualColumn(lineText, logStart);
                    int visEnd = TextLineViewModel.LogicalToVisualColumn(lineText, logEnd);
                    converted.Add(new ReferenceSegment
                    {
                        StartOffset = visStart,
                        EndOffset = visEnd,
                        Reference = seg.Reference,
                        IsLocal = seg.IsLocal,
                    });
                }
                lineRefs = converted;
            }
        }

        return new TextLineViewModel(
            line.LineNumber,
            displayText,
            isCaretLine && _caretVisible ? 1d : 0d,
            isCaretLine ? 0.18d : 0d,
            new Thickness(caretLeft, 0, 0, 0),
            new Thickness(selectionLeft, 0, 0, 0),
            selectionWidth,
            selectionOpacity,
            new Thickness(preeditUnderlineLeft, 0, 0, 0),
            preeditUnderlineWidth,
            preeditUnderlineOpacity,
            Theme,
            WordWrap,
            (LineNumbersForeground as SolidColorBrush)?.Color,
            (SelectionBrush as SolidColorBrush)?.Color,
            (SelectionBorder as SolidColorBrush)?.Color,
            SelectionCornerRadius,
            foldMarker,
            highlightedLine,
            lineRefs,
            preeditVisualStart,
            preeditVisualEnd);
    }

    private void PublishVisibleLinesState(int firstVisualRow, int lastVisualRow)
    {
        bool shouldPublish = !_visibleLinesPublished
            || _prevFirstVisualRow != firstVisualRow
            || _prevLastVisualRow != lastVisualRow;
        if (!shouldPublish)
        {
            return;
        }

        _visibleLinesPublished = true;
        LogRender($"publish-visible rows={firstVisualRow}-{lastVisualRow} docLines={_firstVisibleLineNumber}-{_lastVisibleLineNumber}");
        VisibleLinesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void PublishScrollOffset()
    {
        double horizontalOffset = TextScrollViewer.HorizontalOffset;
        double verticalOffset = TextScrollViewer.VerticalOffset;
        if (Math.Abs(horizontalOffset - _lastPublishedHorizontalOffset) < 0.001
            && Math.Abs(verticalOffset - _lastPublishedVerticalOffset) < 0.001)
        {
            return;
        }

        _lastPublishedHorizontalOffset = horizontalOffset;
        _lastPublishedVerticalOffset = verticalOffset;
        LogRender($"publish-scroll h={horizontalOffset:0.###} v={verticalOffset:0.###}");
        ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LogVisibleLineNumbers(string phase, IReadOnlyList<TextLineViewModel> lines)
    {
        if (!HighlightLogger.Enabled || lines.Count == 0)
        {
            return;
        }

        int previewCount = Math.Min(lines.Count, 6);
        var preview = new string[previewCount];
        for (int i = 0; i < previewCount; i++)
        {
            preview[i] = $"{i}:{lines[i].LineNumber}";
        }

        string suffix = lines.Count > previewCount ? ",..." : string.Empty;
        LogRender($"{phase} vm-line-numbers [{string.Join(",", preview)}{suffix}]");
    }

    private void GetPreeditVisualRange(DocumentLine line, string lineText, out int visualStart, out int visualEnd)
    {
        visualStart = -1;
        visualEnd = -1;

        if (!_isComposing || _compositionLength <= 0)
        {
            return;
        }

        int compositionStart = _compositionStartOffset;
        int compositionEnd = compositionStart + _compositionLength;
        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;
        int segmentStart = Math.Max(compositionStart, lineStart);
        int segmentEnd = Math.Min(compositionEnd, lineEnd);
        if (segmentStart >= segmentEnd)
        {
            return;
        }

        int logicalStart = Math.Clamp(segmentStart - lineStart, 0, lineText.Length);
        int logicalEnd = Math.Clamp(segmentEnd - lineStart, logicalStart, lineText.Length);
        visualStart = TextLineViewModel.LogicalToVisualColumn(lineText, logicalStart);
        visualEnd = TextLineViewModel.LogicalToVisualColumn(lineText, logicalEnd);
    }

    private void GetPreeditUnderlineLayout(DocumentLine line, string lineText, out double left, out double width, out double opacity)
    {
        left = 0d;
        width = 0d;
        opacity = 0d;

        if (!_isComposing || _compositionLength <= 0)
        {
            return;
        }

        int compositionStart = _compositionStartOffset;
        int compositionEnd = compositionStart + _compositionLength;
        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;
        int segmentStart = Math.Max(compositionStart, lineStart);
        int segmentEnd = Math.Min(compositionEnd, lineEnd);
        if (segmentStart >= segmentEnd)
        {
            return;
        }

        int logicalStart = Math.Clamp(segmentStart - lineStart, 0, lineText.Length);
        int logicalEnd = Math.Clamp(segmentEnd - lineStart, logicalStart, lineText.Length);
        double startX = GetDisplayColumnX(lineText, logicalStart);
        double endX = GetDisplayColumnX(lineText, logicalEnd);
        left = Math.Max(0d, startX);
        width = Math.Max(1d, endX - startX);
        opacity = 1d;
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

    /// <summary>Determine the fold-marker kind for a document line.</summary>
    private FoldMarkerKind GetFoldMarkerKind(DocumentLine line)
    {
        var fm = FoldingManager;
        if (fm is null) return FoldMarkerKind.None;

        // Priority 1: a fold starts on this line (box marker)
        foreach (var section in fm.AllFoldings)
        {
            if (section.StartOffset >= line.Offset && section.StartOffset <= line.EndOffset)
                return section.IsFolded ? FoldMarkerKind.CanExpand : FoldMarkerKind.CanFold;
        }

        // Priority 2: an expanded fold ends on this line (L-shaped end marker)
        foreach (var section in fm.AllFoldings)
        {
            if (!section.IsFolded && section.EndOffset >= line.Offset && section.EndOffset <= line.EndOffset
                && section.StartOffset < line.Offset)
                return FoldMarkerKind.FoldEnd;
        }

        // Priority 3: inside an expanded fold region (vertical connecting line)
        foreach (var section in fm.AllFoldings)
        {
            if (!section.IsFolded && section.StartOffset < line.Offset && section.EndOffset > line.EndOffset)
                return FoldMarkerKind.InsideFold;
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

        // `point` is measured relative to the content StackPanel (which already includes
        // TopSpacer.Height). Adding `TextScrollViewer.VerticalOffset` double-counts the
        // scroll offset; use the content-relative Y directly.
        double absoluteY = y;
        int visualRow = Math.Clamp((int)(absoluteY / LineHeight), 0, _visibleDocLines.Count - 1);
        int targetLine = _visibleDocLines.Count > 0 ? _visibleDocLines[visualRow] : 1;
        DocumentLine documentLine = _document.GetLineByNumber(targetLine);

        // Layout: [40 line-nums?][16 fold-margin] = 56 with line nums, 16 without.
        double gutterOffset = ShowLineNumbers ? GutterWidth : 16d;
        double documentX = x + TextScrollViewer.HorizontalOffset - gutterOffset - TextLeftPadding;
        string lineText = _document.GetText(documentLine);
        int logicalColumn = GetLogicalColumnFromDisplayX(lineText, documentX);
        int targetColumn = Math.Clamp(logicalColumn + 1, 1, documentLine.Length + 1);
        _desiredColumn = targetColumn;
        int offset = _document.GetOffset(targetLine, targetColumn);
        LogRender($"hit-test x={x:0.###} y={y:0.###} absY={absoluteY:0.###} visualRow={visualRow} targetLine={targetLine} docX={documentX:0.###} logicalColumn={logicalColumn} targetColumn={targetColumn} lineLength={documentLine.Length} offset={offset}");
        return offset;
    }

    partial void InitializePlatformInputBridge();
    partial void UpdatePlatformInputBridge();
    partial void FocusPlatformInputBridge();
    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed);

}
