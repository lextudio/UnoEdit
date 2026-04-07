using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Document;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView : UserControl
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(
            nameof(Document),
            typeof(TextDocument),
            typeof(TextView),
            new PropertyMetadata(null, OnDocumentChanged));

    private readonly ObservableCollection<TextLineViewModel> _lines = new();
    private TextDocument? _document;

    public TextView()
    {
        this.InitializeComponent();
        LinesItemsControl.ItemsSource = _lines;
    }

    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    private static void OnDocumentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var textView = (TextView)dependencyObject;
        textView.AttachDocument(args.OldValue as TextDocument, args.NewValue as TextDocument);
    }

    private void AttachDocument(TextDocument? oldDocument, TextDocument? newDocument)
    {
        if (oldDocument is not null)
        {
            oldDocument.TextChanged -= HandleDocumentTextChanged;
        }

        _document = newDocument;

        if (newDocument is not null)
        {
            newDocument.TextChanged += HandleDocumentTextChanged;
        }

        RebuildLines();
    }

    private void HandleDocumentTextChanged(object? sender, EventArgs e)
    {
        RebuildLines();
    }

    private void RebuildLines()
    {
        _lines.Clear();

        if (_document is null)
        {
            return;
        }

        foreach (DocumentLine line in _document.Lines)
        {
            string lineText = _document.GetText(line);
            _lines.Add(new TextLineViewModel(line.LineNumber, lineText.Length == 0 ? " " : lineText));
        }
    }
}

public sealed class TextLineViewModel
{
    public TextLineViewModel(int number, string text)
    {
        Number = number.ToString();
        Text = text;
    }

    public string Number { get; }

    public string Text { get; }
}
