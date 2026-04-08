using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextArea : UserControl
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

    public event EventHandler? CaretOffsetChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<ReferenceSegment>? NavigationRequested;

    public TextArea()
    {
        this.InitializeComponent();
        PART_TextView.CaretOffsetChanged  += OnTextViewCaretOffsetChanged;
        PART_TextView.SelectionChanged    += OnTextViewSelectionChanged;
        PART_TextView.NavigationRequested += (s, e) => NavigationRequested?.Invoke(this, e);
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
        get => PART_TextView.ReferenceSegmentSource;
        set => PART_TextView.ReferenceSegmentSource = value;
    }

    public ICSharpCode.AvalonEdit.Folding.FoldingManager? FoldingManager
    {
        get => PART_TextView.FoldingManager;
        set => PART_TextView.FoldingManager = value;
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Document = args.NewValue as TextDocument;
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Theme = (args.NewValue as TextEditorTheme) ?? TextEditorTheme.Dark;
    }

    public void ScrollToLine(int lineNumber)
    {
        PART_TextView.ScrollToLine(lineNumber);
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
}
