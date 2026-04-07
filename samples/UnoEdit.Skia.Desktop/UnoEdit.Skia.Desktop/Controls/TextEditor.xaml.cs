using ICSharpCode.AvalonEdit.Document;

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

    private TextDocument? _attachedDocument;

    public TextEditor()
    {
        this.InitializeComponent();
        PART_TextArea.CaretOffsetChanged += OnTextAreaCaretOffsetChanged;
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
        var editor = (TextEditor)dependencyObject;
        editor.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
        editor.PART_TextArea.Document = args.NewValue as TextDocument;
        editor.UpdateSummary();
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

    internal void UpdateSummary()
    {
        if (Document is null)
        {
            SummaryTextBlock.Text = "No document";
            return;
        }

        TextLocation location = Document.GetLocation(CurrentOffset);
        SummaryTextBlock.Text = $"{Document.LineCount} lines  {Document.TextLength} chars  Ln {location.Line}, Col {location.Column}";
    }
}
