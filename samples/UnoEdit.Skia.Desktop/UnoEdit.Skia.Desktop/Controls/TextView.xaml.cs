using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Xaml.Input;

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

    private readonly ObservableCollection<TextLineViewModel> _lines = new();
    private const double LineHeight = 22d;
    private const double CharacterWidth = 8.4d;
    private const double TextLeftPadding = 0d;
    private const double GutterWidth = 72d;
    private const int OverscanLineCount = 4;
    private TextDocument? _document;
    private int _firstVisibleLineNumber = 1;
    private int _lastVisibleLineNumber;
    private int _desiredColumn = 1;

    public event EventHandler? CaretOffsetChanged;

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

    public int CurrentOffset
    {
        get => (int)GetValue(CurrentOffsetProperty);
        set => SetValue(CurrentOffsetProperty, value);
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
            CurrentOffset = Math.Min(CurrentOffset, newDocument.TextLength);
        }
        else
        {
            CurrentOffset = 0;
        }

        RefreshViewport();
    }

    private void HandleDocumentTextChanged(object? sender, EventArgs e)
    {
        if (_document is not null && CurrentOffset > _document.TextLength)
        {
            CurrentOffset = _document.TextLength;
        }

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

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        Focus(FocusState.Programmatic);

        var point = e.GetCurrentPoint(ContentStackPanel).Position;
        int targetLine = ClampLineNumber(((int)((point.Y + TextScrollViewer.VerticalOffset) / LineHeight)) + 1);
        DocumentLine documentLine = _document.GetLineByNumber(targetLine);

        double documentX = point.X + TextScrollViewer.HorizontalOffset - GutterWidth - TextLeftPadding;
        int targetColumn = Math.Max(1, ((int)(documentX / CharacterWidth)) + 1);
        targetColumn = ClampColumn(documentLine, targetColumn);

        _desiredColumn = targetColumn;
        CurrentOffset = _document.GetOffset(targetLine, targetColumn);
        e.Handled = true;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        bool handled = e.Key switch
        {
            Windows.System.VirtualKey.Left => MoveHorizontal(-1),
            Windows.System.VirtualKey.Right => MoveHorizontal(1),
            Windows.System.VirtualKey.Up => MoveVertical(-1),
            Windows.System.VirtualKey.Down => MoveVertical(1),
            Windows.System.VirtualKey.Home => MoveToLineBoundary(true),
            Windows.System.VirtualKey.End => MoveToLineBoundary(false),
            _ => false
        };

        e.Handled = handled;
    }

    private bool MoveHorizontal(int delta)
    {
        if (_document is null)
        {
            return false;
        }

        int targetOffset = Math.Clamp(CurrentOffset + delta, 0, _document.TextLength);
        CurrentOffset = targetOffset;
        _desiredColumn = _document.GetLocation(CurrentOffset).Column;
        return true;
    }

    private bool MoveVertical(int delta)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        int targetLineNumber = ClampLineNumber(location.Line + delta);
        DocumentLine targetLine = _document.GetLineByNumber(targetLineNumber);
        int targetColumn = ClampColumn(targetLine, _desiredColumn);
        CurrentOffset = _document.GetOffset(targetLineNumber, targetColumn);
        return true;
    }

    private bool MoveToLineBoundary(bool moveToStart)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        DocumentLine line = _document.GetLineByNumber(location.Line);
        int targetColumn = moveToStart ? 1 : line.Length + 1;
        _desiredColumn = targetColumn;
        CurrentOffset = _document.GetOffset(location.Line, targetColumn);
        return true;
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
        CaretOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureCaretVisible()
    {
        if (_document is null)
        {
            return;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        double targetTop = Math.Max(0, (location.Line - 1) * LineHeight);
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
            bool isCaretLine = line.Offset <= CurrentOffset && CurrentOffset <= line.EndOffset;
            int caretColumn = isCaretLine ? _document.GetLocation(CurrentOffset).Column : 1;
            double caretLeft = Math.Max(0, (caretColumn - 1) * CharacterWidth);

            _lines.Add(new TextLineViewModel(
                line.LineNumber,
                lineText.Length == 0 ? " " : lineText,
                isCaretLine ? 1d : 0d,
                isCaretLine ? 0.18d : 0d,
                new Thickness(caretLeft, 0, 0, 0)));
        }

        TopSpacer.Height = (firstVisibleLine - 1) * LineHeight;
        BottomSpacer.Height = Math.Max(0, (lineCount - lastVisibleLine) * LineHeight);
    }

    private int ClampLineNumber(int lineNumber)
    {
        return _document is null ? 1 : Math.Clamp(lineNumber, 1, _document.LineCount);
    }

    private static int ClampColumn(DocumentLine line, int column)
    {
        return Math.Clamp(column, 1, line.Length + 1);
    }
}

public sealed class TextLineViewModel
{
    public TextLineViewModel(int number, string text, double caretOpacity, double highlightOpacity, Thickness caretMargin)
    {
        Number = number.ToString();
        Text = text;
        CaretOpacity = caretOpacity;
        HighlightOpacity = highlightOpacity;
        CaretMargin = caretMargin;
    }

    public string Number { get; }

    public string Text { get; }

    public double CaretOpacity { get; }

    public double HighlightOpacity { get; }

    public Thickness CaretMargin { get; }
}
