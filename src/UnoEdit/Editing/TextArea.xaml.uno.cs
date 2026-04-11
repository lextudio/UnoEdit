using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Input;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextArea : UserControl, IServiceProvider
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(
            nameof(Document),
            typeof(TextDocument),
            typeof(TextArea),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CurrentOffsetProperty =
        DependencyProperty.Register(
            nameof(CurrentOffset),
            typeof(int),
            typeof(TextArea),
            new PropertyMetadata(0, OnCurrentOffsetChanged));

    public static readonly DependencyProperty SelectionStartOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionStartOffset),
            typeof(int),
            typeof(TextArea),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty SelectionEndOffsetProperty =
        DependencyProperty.Register(
            nameof(SelectionEndOffset),
            typeof(int),
            typeof(TextArea),
            new PropertyMetadata(0, OnSelectionRangeChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(
            nameof(Theme),
            typeof(TextEditorTheme),
            typeof(TextArea),
            new PropertyMetadata(TextEditorTheme.Dark, OnThemeChanged));

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options),
            typeof(TextEditorOptions),
            typeof(TextArea),
            new PropertyMetadata(new TextEditorOptions(), OnOptionsChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(TextArea),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    public static readonly DependencyProperty ShowLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowLineNumbers),
            typeof(bool),
            typeof(TextArea),
            new PropertyMetadata(true, OnShowLineNumbersChanged));

    public static readonly DependencyProperty WordWrapProperty =
        DependencyProperty.Register(
            nameof(WordWrap),
            typeof(bool),
            typeof(TextArea),
            new PropertyMetadata(false, OnWordWrapChanged));

    public static readonly DependencyProperty IndentationStrategyProperty =
        DependencyProperty.Register(
            nameof(IndentationStrategy),
            typeof(IIndentationStrategy),
            typeof(TextArea),
            new PropertyMetadata(null, OnIndentationStrategyChanged));

    public static readonly DependencyProperty OverstrikeModeProperty =
        DependencyProperty.Register(
            nameof(OverstrikeMode),
            typeof(bool),
            typeof(TextArea),
            new PropertyMetadata(false, OnOverstrikeModeChanged));

    public static readonly DependencyProperty LineNumbersForegroundProperty =
        DependencyProperty.Register(
            nameof(LineNumbersForeground),
            typeof(Brush),
            typeof(TextArea),
            new PropertyMetadata(null, OnLineNumbersForegroundChanged));

    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(TextArea),
            new PropertyMetadata(null, OnSelectionBrushChanged));

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register(
            nameof(SelectionForeground),
            typeof(Brush),
            typeof(TextArea),
            new PropertyMetadata(null, OnSelectionForegroundChanged));

    public static readonly DependencyProperty SelectionBorderProperty =
        DependencyProperty.Register(
            nameof(SelectionBorder),
            typeof(Brush),
            typeof(TextArea),
            new PropertyMetadata(null, OnSelectionBorderChanged));

    public static readonly DependencyProperty SelectionCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(SelectionCornerRadius),
            typeof(double),
            typeof(TextArea),
            new PropertyMetadata(0d, OnSelectionCornerRadiusChanged));

    public event EventHandler? DocumentChanged;
    public event PropertyChangedEventHandler? OptionChanged;
    public event EventHandler? CaretOffsetChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<ReferenceSegment>? NavigationRequested;
    public event EventHandler<TextEventArgs>? TextCopied;

    /// <summary>Raised after text has been entered into the editor.</summary>
    public event EventHandler<TextCompositionEventArgs>? TextEntered;

    /// <summary>Raised before text is entered into the editor, allowing the handler to preview or cancel.</summary>
    public event EventHandler<TextCompositionEventArgs>? TextEntering;

    private Caret _caret;

    /// <summary>Gets the caret used by this text area.</summary>
    public Caret Caret => _caret;

    /// <summary>
    /// Gets or sets whether the caret is allowed to be placed outside of the selection.
    /// </summary>
    public bool AllowCaretOutsideSelection { get; set; } = true;

    /// <summary>
    /// Gets the current mouse selection mode.
    /// </summary>
    public MouseSelectionMode MouseSelectionMode { get; internal set; } = MouseSelectionMode.None;

    public TextArea()
    {
        this.InitializeComponent();
        _caret = new Caret(
            bringIntoView: () => ScrollToLine(_caret.Line),
            setOffset: offset => {
                if (offset >= 0)
                    CurrentOffset = offset;
                else {
                    // Negative sentinel encodes line*100000+col, written by Position setter
                    int encoded = -offset;
                    int line = encoded / 100000;
                    int col  = encoded % 100000;
                    var doc = Document;
                    if (doc != null && line >= 1 && line <= doc.LineCount)
                        CurrentOffset = doc.GetOffset(line, col);
                }
            });
        DefaultInputHandler = new TextAreaDefaultInputHandler(this);
        ActiveInputHandler  = DefaultInputHandler;
        PART_TextView.Services.AddService(typeof(TextArea), this);
        PART_TextView.CaretOffsetChanged  += OnTextViewCaretOffsetChanged;
        PART_TextView.SelectionChanged    += OnTextViewSelectionChanged;
        PART_TextView.NavigationRequested += (s, e) => NavigationRequested?.Invoke(this, e);
        PART_TextView.TextCopied          += (s, e) => TextCopied?.Invoke(this, e);
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

    public IReadOnlySectionProvider? ReadOnlySectionProvider
    {
        get => PART_TextView.ReadOnlySectionProvider;
        set => PART_TextView.ReadOnlySectionProvider = value;
    }

    /// <summary>Provides access to the inner TextView for testing and advanced scenarios.</summary>
    public TextView TextView => PART_TextView;

    public IReferenceSegmentSource? ReferenceSegmentSource
    {
        get => PART_TextView.ReferenceSegmentSource;
        set => PART_TextView.ReferenceSegmentSource = value;
    }

    public ICSharpCode.AvalonEdit.Folding.FoldingManager? FoldingManager
    {
        get => PART_TextView.FoldingManager;
        set => PART_TextView.FoldingManager = value;
    }

    public IHighlightedLineSource? HighlightedLineSource
    {
        get => PART_TextView.HighlightedLineSource;
        set => PART_TextView.HighlightedLineSource = value;
    }

    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => PART_TextView.SyntaxHighlighting;
        set => PART_TextView.SyntaxHighlighting = value;
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Document = args.NewValue as TextDocument;
        textArea.DocumentChanged?.Invoke(textArea, EventArgs.Empty);
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Theme = (args.NewValue as TextEditorTheme) ?? TextEditorTheme.Dark;
    }

    private static void OnOptionsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Options = (args.NewValue as TextEditorOptions) ?? new TextEditorOptions();
        textArea.OptionChanged?.Invoke(textArea, new PropertyChangedEventArgs(null));
    }

    private static void OnIsReadOnlyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.IsReadOnly = (bool)args.NewValue;
    }

    private static void OnShowLineNumbersChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.ShowLineNumbers = (bool)args.NewValue;
    }

    private static void OnWordWrapChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.WordWrap = (bool)args.NewValue;
    }

    private static void OnIndentationStrategyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.IndentationStrategy = args.NewValue as IIndentationStrategy;
    }

    private static void OnOverstrikeModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.OverstrikeMode = (bool)args.NewValue;
    }

    private static void OnLineNumbersForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.LineNumbersForeground = args.NewValue as Brush;
    }

    private static void OnSelectionBrushChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.SelectionBrush = args.NewValue as Brush;
    }

    private static void OnSelectionForegroundChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.SelectionForeground = args.NewValue as Brush;
    }

    private static void OnSelectionBorderChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.SelectionBorder = args.NewValue as Brush;
    }

    private static void OnSelectionCornerRadiusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.SelectionCornerRadius = (double)args.NewValue;
    }

    public void ScrollToLine(int lineNumber)
    {
        PART_TextView.ScrollToLine(lineNumber);
    }

    public void ClearSelection()
    {
        PART_TextView.ClearSelection();
    }

    public object? GetService(Type serviceType)
    {
        return PART_TextView.GetService(serviceType);
    }

    private static void OnCurrentOffsetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        if (textArea.PART_TextView.CurrentOffset != (int)args.NewValue)
        {
            textArea.PART_TextView.CurrentOffset = (int)args.NewValue;
        }
    }

    private static void OnSelectionRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        if (textArea.PART_TextView.SelectionStartOffset != textArea.SelectionStartOffset)
        {
            textArea.PART_TextView.SelectionStartOffset = textArea.SelectionStartOffset;
        }

        if (textArea.PART_TextView.SelectionEndOffset != textArea.SelectionEndOffset)
        {
            textArea.PART_TextView.SelectionEndOffset = textArea.SelectionEndOffset;
        }
    }

    private void OnTextViewCaretOffsetChanged(object? sender, EventArgs e)
    {
        if (CurrentOffset != PART_TextView.CurrentOffset)
        {
            CurrentOffset = PART_TextView.CurrentOffset;
        }

        // Keep the Caret facade in sync.
        var doc = Document;
        if (doc != null && CurrentOffset >= 0 && CurrentOffset <= doc.TextLength)
        {
            var loc = doc.GetLocation(CurrentOffset);
            _caret?.Update(CurrentOffset, loc.Line, loc.Column);
        }

        CaretOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTextViewSelectionChanged(object? sender, EventArgs e)
    {
        if (SelectionStartOffset != PART_TextView.SelectionStartOffset)
        {
            SelectionStartOffset = PART_TextView.SelectionStartOffset;
        }

        if (SelectionEndOffset != PART_TextView.SelectionEndOffset)
        {
            SelectionEndOffset = PART_TextView.SelectionEndOffset;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // ----------------------------------------------------------------
    // Input handler surface
    // ----------------------------------------------------------------
    private ITextAreaInputHandler? _activeInputHandler;

    public TextAreaDefaultInputHandler DefaultInputHandler { get; private set; }
    public ITextAreaInputHandler ActiveInputHandler
    {
        get => _activeInputHandler!;
        set
        {
            if (ReferenceEquals(_activeInputHandler, value))
            {
                return;
            }

            _activeInputHandler?.Detach();
            _activeInputHandler = value ?? throw new ArgumentNullException(nameof(value));
            _activeInputHandler.Attach();
            ActiveInputHandlerChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler ActiveInputHandlerChanged;
    public System.Collections.Immutable.ImmutableStack<ITextAreaInputHandler> StackedInputHandlers { get; private set; }
        = System.Collections.Immutable.ImmutableStack<ITextAreaInputHandler>.Empty;

    public void PushStackedInputHandler(ITextAreaInputHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        handler.Attach();
        StackedInputHandlers = StackedInputHandlers.Push(handler);
    }

    public void PopStackedInputHandler(ITextAreaInputHandler handler)
    {
        if (!StackedInputHandlers.IsEmpty)
        {
            StackedInputHandlers = StackedInputHandlers.Pop();
            handler?.Detach();
        }
    }

    public void PerformTextInput(string text)
    {
        // Forward text insertion to the document editor
        if (Document != null && !IsReadOnly)
        {
            int offset = CurrentOffset;
            Document.Insert(offset, text);
        }
    }

    // ----------------------------------------------------------------
    // Selection surface property (returns selection state as object until
    // a full Selection class is implemented)
    // ----------------------------------------------------------------
    public object Selection { get; internal set; } = null;

    // ----------------------------------------------------------------
    // Left margins collection
    // ----------------------------------------------------------------
    public System.Collections.ObjectModel.ObservableCollection<UIElement> LeftMargins { get; }
        = new System.Collections.ObjectModel.ObservableCollection<UIElement>();

	public new void OnApplyTemplate() { base.OnApplyTemplate(); }
}
