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
    private const double LineHeight = 22d;
    private const int OverscanLineCount = 4;
    private TextDocument? _document;
    private int _firstVisibleLineNumber = 1;
    private int _lastVisibleLineNumber;

    public TextView()
    {
        this.InitializeComponent();
        LinesItemsControl.ItemsSource = _lines;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
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

        RefreshViewport();
    }

    private void HandleDocumentTextChanged(object? sender, EventArgs e)
    {
        RefreshViewport();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshViewport();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RefreshViewport();
    }

    private void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        RefreshViewport();
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

        int lineCount = _document.LineCount;
        double verticalOffset = TextScrollViewer.VerticalOffset;
        double viewportHeight = TextScrollViewer.ViewportHeight;
        if (viewportHeight <= 0)
        {
            viewportHeight = ActualHeight > 0 ? ActualHeight : 400;
        }

        int visibleLineCount = Math.Max(1, (int)Math.Ceiling(viewportHeight / LineHeight) + (OverscanLineCount * 2));
        int firstVisibleLine = Math.Max(1, ((int)(verticalOffset / LineHeight) + 1) - OverscanLineCount);
        int lastVisibleLine = Math.Min(lineCount, firstVisibleLine + visibleLineCount - 1);

        if (firstVisibleLine == _firstVisibleLineNumber && lastVisibleLine == _lastVisibleLineNumber)
        {
            return;
        }

        _firstVisibleLineNumber = firstVisibleLine;
        _lastVisibleLineNumber = lastVisibleLine;

        _lines.Clear();
        for (int lineNumber = firstVisibleLine; lineNumber <= lastVisibleLine; lineNumber++)
        {
            DocumentLine line = _document.GetLineByNumber(lineNumber);
            string lineText = _document.GetText(line);
            _lines.Add(new TextLineViewModel(line.LineNumber, lineText.Length == 0 ? " " : lineText));
        }

        TopSpacer.Height = (firstVisibleLine - 1) * LineHeight;
        BottomSpacer.Height = Math.Max(0, (lineCount - lastVisibleLine) * LineHeight);
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
