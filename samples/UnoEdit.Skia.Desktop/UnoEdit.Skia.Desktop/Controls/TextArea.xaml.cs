using ICSharpCode.AvalonEdit.Document;

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

    public event EventHandler? CaretOffsetChanged;

    public TextArea()
    {
        this.InitializeComponent();
        PART_TextView.CaretOffsetChanged += OnTextViewCaretOffsetChanged;
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

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        textArea.PART_TextView.Document = args.NewValue as TextDocument;
    }

    private static void OnCurrentOffsetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textArea = (TextArea)dependencyObject;
        if (textArea.PART_TextView.CurrentOffset != (int)args.NewValue)
        {
            textArea.PART_TextView.CurrentOffset = (int)args.NewValue;
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
}
