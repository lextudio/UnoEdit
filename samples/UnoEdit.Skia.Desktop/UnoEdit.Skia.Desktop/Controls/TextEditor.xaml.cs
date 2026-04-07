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

    public TextEditor()
    {
        this.InitializeComponent();
    }

    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var editor = (TextEditor)dependencyObject;
        editor.PART_TextArea.Document = args.NewValue as TextDocument;
        editor.UpdateSummary();
    }

    internal void UpdateSummary()
    {
        if (Document is null)
        {
            SummaryTextBlock.Text = "No document";
            return;
        }

        SummaryTextBlock.Text = $"{Document.LineCount} lines  {Document.TextLength} chars";
    }
}
