using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Input;
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
    private int _selectionAnchorOffset;
    private bool _isPointerSelecting;

    public event EventHandler? CaretOffsetChanged;
    public event EventHandler? SelectionChanged;

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

        RefreshViewport();
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
        int targetOffset = GetOffsetFromViewPoint(point.X, point.Y);
        bool extendSelection = IsShiftPressed();

        _isPointerSelecting = true;
        CapturePointer(e.Pointer);

        UpdateCaretAndSelection(targetOffset, extendSelection);
        if (!extendSelection)
        {
            _selectionAnchorOffset = targetOffset;
        }

        e.Handled = true;
    }

    private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null || !_isPointerSelecting)
        {
            return;
        }

        var point = e.GetCurrentPoint(ContentStackPanel).Position;
        int targetOffset = GetOffsetFromViewPoint(point.X, point.Y);

        CurrentOffset = targetOffset;
        SelectionStartOffset = Math.Min(_selectionAnchorOffset, targetOffset);
        SelectionEndOffset = Math.Max(_selectionAnchorOffset, targetOffset);
        e.Handled = true;
    }

    private void OnRootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerSelecting)
        {
            return;
        }

        _isPointerSelecting = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        bool extendSelection = IsShiftPressed();
        bool handled = e.Key switch
        {
            Windows.System.VirtualKey.Left => MoveHorizontal(-1, extendSelection),
            Windows.System.VirtualKey.Right => MoveHorizontal(1, extendSelection),
            Windows.System.VirtualKey.Up => MoveVertical(-1, extendSelection),
            Windows.System.VirtualKey.Down => MoveVertical(1, extendSelection),
            Windows.System.VirtualKey.Home => MoveToLineBoundary(true, extendSelection),
            Windows.System.VirtualKey.End => MoveToLineBoundary(false, extendSelection),
            _ => false
        };

        e.Handled = handled;
    }

    private bool MoveHorizontal(int delta, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        int targetOffset = Math.Clamp(CurrentOffset + delta, 0, _document.TextLength);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    private bool MoveVertical(int delta, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        int targetLineNumber = ClampLineNumber(location.Line + delta);
        DocumentLine targetLine = _document.GetLineByNumber(targetLineNumber);
        int targetColumn = ClampColumn(targetLine, _desiredColumn);
        int targetOffset = _document.GetOffset(targetLineNumber, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    private bool MoveToLineBoundary(bool moveToStart, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        DocumentLine line = _document.GetLineByNumber(location.Line);
        int targetColumn = moveToStart ? 1 : line.Length + 1;
        int targetOffset = _document.GetOffset(location.Line, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
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

        _firstVisibleLineNumber = firstVisibleLine;
        _lastVisibleLineNumber = lastVisibleLine;

        _lines.Clear();
        int selectionStart = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int selectionEnd = Math.Max(SelectionStartOffset, SelectionEndOffset);
        bool hasSelection = selectionStart != selectionEnd;

        for (int lineNumber = firstVisibleLine; lineNumber <= lastVisibleLine; lineNumber++)
        {
            DocumentLine line = _document.GetLineByNumber(lineNumber);
            string lineText = _document.GetText(line);
            bool isCaretLine = line.Offset <= CurrentOffset && CurrentOffset <= line.EndOffset;
            int caretColumn = isCaretLine ? _document.GetLocation(CurrentOffset).Column : 1;
            double caretLeft = Math.Max(0, (caretColumn - 1) * CharacterWidth);
            double selectionOpacity = 0d;
            double selectionLeft = 0d;
            double selectionWidth = 0d;

            if (hasSelection)
            {
                int lineSelectionStart = Math.Max(selectionStart, line.Offset);
                int lineSelectionEnd = Math.Min(selectionEnd, line.EndOffset);
                if (lineSelectionStart < lineSelectionEnd)
                {
                    int startColumn = (lineSelectionStart - line.Offset) + 1;
                    int endColumn = (lineSelectionEnd - line.Offset) + 1;
                    selectionLeft = Math.Max(0, (startColumn - 1) * CharacterWidth);
                    selectionWidth = Math.Max(2, (endColumn - startColumn) * CharacterWidth);
                    selectionOpacity = 0.45d;
                }
            }

            _lines.Add(new TextLineViewModel(
                line.LineNumber,
                lineText.Length == 0 ? " " : lineText,
                isCaretLine ? 1d : 0d,
                isCaretLine ? 0.18d : 0d,
                new Thickness(caretLeft, 0, 0, 0),
                new Thickness(selectionLeft, 0, 0, 0),
                selectionWidth,
                selectionOpacity));
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

    private int GetOffsetFromViewPoint(double x, double y)
    {
        if (_document is null)
        {
            return 0;
        }

        int targetLine = ClampLineNumber(((int)((y + TextScrollViewer.VerticalOffset) / LineHeight)) + 1);
        DocumentLine documentLine = _document.GetLineByNumber(targetLine);

        double documentX = x + TextScrollViewer.HorizontalOffset - GutterWidth - TextLeftPadding;
        int targetColumn = Math.Max(1, ((int)(documentX / CharacterWidth)) + 1);
        targetColumn = ClampColumn(documentLine, targetColumn);
        _desiredColumn = targetColumn;
        return _document.GetOffset(targetLine, targetColumn);
    }

    private void UpdateCaretAndSelection(int targetOffset, bool extendSelection)
    {
        CurrentOffset = targetOffset;

        if (extendSelection)
        {
            SelectionStartOffset = Math.Min(_selectionAnchorOffset, targetOffset);
            SelectionEndOffset = Math.Max(_selectionAnchorOffset, targetOffset);
        }
        else
        {
            _selectionAnchorOffset = targetOffset;
            SelectionStartOffset = targetOffset;
            SelectionEndOffset = targetOffset;
        }

        _desiredColumn = _document?.GetLocation(CurrentOffset).Column ?? 1;
    }

    private static bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }
}

public sealed class TextLineViewModel
{
    public TextLineViewModel(
        int number,
        string text,
        double caretOpacity,
        double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin,
        double selectionWidth,
        double selectionOpacity)
    {
        Number = number.ToString();
        Text = text;
        CaretOpacity = caretOpacity;
        HighlightOpacity = highlightOpacity;
        CaretMargin = caretMargin;
        SelectionMargin = selectionMargin;
        SelectionWidth = selectionWidth;
        SelectionOpacity = selectionOpacity;
    }

    public string Number { get; }

    public string Text { get; }

    public double CaretOpacity { get; }

    public double HighlightOpacity { get; }

    public Thickness CaretMargin { get; }

    public Thickness SelectionMargin { get; }

    public double SelectionWidth { get; }

    public double SelectionOpacity { get; }
}
