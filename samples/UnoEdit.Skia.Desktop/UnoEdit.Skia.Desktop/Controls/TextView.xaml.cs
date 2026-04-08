using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using System.Windows.Documents;
using Windows.ApplicationModel.DataTransfer;

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

    private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        bool extendSelection = IsShiftPressed();
        bool controlPressed = IsControlPressed();
        string? inputText = !controlPressed ? TranslateKeyToText(e.Key, extendSelection) : null;
        bool handled = e.Key switch
        {
            Windows.System.VirtualKey.A when controlPressed => SelectAll(),
            Windows.System.VirtualKey.C when controlPressed => CopySelection(),
            Windows.System.VirtualKey.Y when controlPressed => Redo(),
            Windows.System.VirtualKey.Z when controlPressed && extendSelection => Redo(),
            Windows.System.VirtualKey.Z when controlPressed => Undo(),
            Windows.System.VirtualKey.X when controlPressed => CutSelection(),
            Windows.System.VirtualKey.Back when controlPressed => DeleteWord(backward: true),
            Windows.System.VirtualKey.Delete when controlPressed => DeleteWord(backward: false),
            Windows.System.VirtualKey.Back => Backspace(),
            Windows.System.VirtualKey.Delete => Delete(),
            Windows.System.VirtualKey.Enter => InsertText(Environment.NewLine),
            Windows.System.VirtualKey.Tab => InsertText("\t"),
            Windows.System.VirtualKey.Left when controlPressed => MoveWordBoundary(backward: true, extendSelection),
            Windows.System.VirtualKey.Right when controlPressed => MoveWordBoundary(backward: false, extendSelection),
            Windows.System.VirtualKey.Left => MoveHorizontal(-1, extendSelection),
            Windows.System.VirtualKey.Right => MoveHorizontal(1, extendSelection),
            Windows.System.VirtualKey.Up => MoveVertical(-1, extendSelection),
            Windows.System.VirtualKey.Down => MoveVertical(1, extendSelection),
            Windows.System.VirtualKey.PageUp => MovePageVertical(-1, extendSelection),
            Windows.System.VirtualKey.PageDown => MovePageVertical(1, extendSelection),
            Windows.System.VirtualKey.Home when controlPressed => MoveToDocumentBoundary(true, extendSelection),
            Windows.System.VirtualKey.End when controlPressed => MoveToDocumentBoundary(false, extendSelection),
            Windows.System.VirtualKey.Home => MoveToLineBoundary(true, extendSelection),
            Windows.System.VirtualKey.End => MoveToLineBoundary(false, extendSelection),
            _ when inputText is not null => InsertText(inputText),
            _ => false
        };

        if (!handled && controlPressed && e.Key == Windows.System.VirtualKey.V)
        {
            handled = await PasteAsync();
        }

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

    private bool MoveToDocumentBoundary(bool moveToStart, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        int targetOffset = moveToStart ? 0 : _document.TextLength;
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    private bool MovePageVertical(int direction, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        double viewportHeight = TextScrollViewer.ViewportHeight > 0 ? TextScrollViewer.ViewportHeight : ActualHeight;
        int pageLines = Math.Max(1, (int)(viewportHeight / LineHeight) - 1);
        TextLocation location = _document.GetLocation(CurrentOffset);
        int targetLineNumber = ClampLineNumber(location.Line + direction * pageLines);
        DocumentLine targetLine = _document.GetLineByNumber(targetLineNumber);
        int targetColumn = ClampColumn(targetLine, _desiredColumn);
        int targetOffset = _document.GetOffset(targetLineNumber, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    private bool MoveWordBoundary(bool backward, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        LogicalDirection dir = backward ? LogicalDirection.Backward : LogicalDirection.Forward;
        int nextOffset = TextUtilities.GetNextCaretPosition(_document, CurrentOffset, dir, CaretPositioningMode.WordStartOrSymbol);
        if (nextOffset < 0)
        {
            nextOffset = backward ? 0 : _document.TextLength;
        }

        UpdateCaretAndSelection(nextOffset, extendSelection);
        return true;
    }

    private bool DeleteWord(bool backward)
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        LogicalDirection dir = backward ? LogicalDirection.Backward : LogicalDirection.Forward;
        int boundary = TextUtilities.GetNextCaretPosition(_document, CurrentOffset, dir, CaretPositioningMode.WordStartOrSymbol);
        if (boundary < 0)
        {
            boundary = backward ? 0 : _document.TextLength;
        }

        int startOffset = backward ? boundary : CurrentOffset;
        int length = Math.Abs(boundary - CurrentOffset);
        if (length == 0)
        {
            return false;
        }

        using (_document.RunUpdate())
        {
            _document.Remove(startOffset, length);
        }

        CollapseSelection(startOffset);
        return true;
    }

    private bool Backspace()
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        if (CurrentOffset == 0)
        {
            return false;
        }

        using (_document.RunUpdate())
        {
            _document.Remove(CurrentOffset - 1, 1);
        }

        CollapseSelection(CurrentOffset - 1);
        return true;
    }

    private bool Delete()
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        if (CurrentOffset >= _document.TextLength)
        {
            return false;
        }

        using (_document.RunUpdate())
        {
            _document.Remove(CurrentOffset, 1);
        }

        CollapseSelection(CurrentOffset);
        return true;
    }

    private bool InsertText(string text)
    {
        if (_document is null)
        {
            return false;
        }

        int insertionOffset = CurrentOffset;
        using (_document.RunUpdate())
        {
            if (HasSelection())
            {
                insertionOffset = DeleteSelectedTextInternal();
            }

            _document.Insert(insertionOffset, text);
        }

        CollapseSelection(insertionOffset + text.Length);
        return true;
    }

    private bool SelectAll()
    {
        if (_document is null)
        {
            return false;
        }

        _selectionAnchorOffset = 0;
        CurrentOffset = _document.TextLength;
        SelectionStartOffset = 0;
        SelectionEndOffset = _document.TextLength;
        return true;
    }

    private bool CopySelection()
    {
        if (_document is null || !HasSelection())
        {
            return false;
        }

        string selectedText = GetSelectedText();
        if (selectedText.Length == 0)
        {
            return false;
        }

        var package = new DataPackage();
        package.SetText(selectedText);
        Clipboard.SetContent(package);
        return true;
    }

    private bool Undo()
    {
        if (_document is null || !_document.UndoStack.CanUndo)
        {
            return false;
        }

        _document.UndoStack.Undo();
        CollapseSelection(Math.Min(CurrentOffset, _document.TextLength));
        return true;
    }

    private bool Redo()
    {
        if (_document is null || !_document.UndoStack.CanRedo)
        {
            return false;
        }

        _document.UndoStack.Redo();
        CollapseSelection(Math.Min(CurrentOffset, _document.TextLength));
        return true;
    }

    private bool CutSelection()
    {
        if (!CopySelection())
        {
            return false;
        }

        DeleteSelectedText();
        return true;
    }

    private async Task<bool> PasteAsync()
    {
        if (_document is null)
        {
            return false;
        }

        DataPackageView package = Clipboard.GetContent();
        if (!package.Contains(StandardDataFormats.Text))
        {
            return false;
        }

        string text = await package.GetTextAsync();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return InsertText(text);
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

    private bool HasSelection()
    {
        return SelectionStartOffset != SelectionEndOffset;
    }

    private string GetSelectedText()
    {
        if (_document is null || !HasSelection())
        {
            return string.Empty;
        }

        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset = Math.Max(SelectionStartOffset, SelectionEndOffset);
        return _document.GetText(startOffset, endOffset - startOffset);
    }

    private void DeleteSelectedText()
    {
        if (_document is null || !HasSelection())
        {
            return;
        }

        int startOffset;
        using (_document.RunUpdate())
        {
            startOffset = DeleteSelectedTextInternal();
        }

        CollapseSelection(startOffset);
    }

    private int DeleteSelectedTextInternal()
    {
        if (_document is null)
        {
            return 0;
        }

        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset = Math.Max(SelectionStartOffset, SelectionEndOffset);
        if (endOffset > startOffset)
        {
            _document.Remove(startOffset, endOffset - startOffset);
        }

        return startOffset;
    }

    private void CollapseSelection(int offset)
    {
        _selectionAnchorOffset = offset;
        CurrentOffset = offset;
        SelectionStartOffset = offset;
        SelectionEndOffset = offset;
    }

    private static bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static bool IsControlPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static string? TranslateKeyToText(Windows.System.VirtualKey key, bool shiftPressed)
    {
        if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
        {
            int delta = key - Windows.System.VirtualKey.A;
            char character = (char)('a' + delta);
            return shiftPressed ? char.ToUpperInvariant(character).ToString() : character.ToString();
        }

        if (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9)
        {
            int delta = key - Windows.System.VirtualKey.Number0;
            return shiftPressed ? ShiftedDigit(delta) : ((char)('0' + delta)).ToString();
        }

        if (key >= Windows.System.VirtualKey.NumberPad0 && key <= Windows.System.VirtualKey.NumberPad9)
        {
            int delta = key - Windows.System.VirtualKey.NumberPad0;
            return ((char)('0' + delta)).ToString();
        }

        return key switch
        {
            Windows.System.VirtualKey.Space => " ",
            // Multiply (*), Add (+), Subtract (-), Decimal (.), Divide (/)
            Windows.System.VirtualKey.Multiply => "*",
            Windows.System.VirtualKey.Add => "+",
            Windows.System.VirtualKey.Subtract => "-",
            Windows.System.VirtualKey.Decimal => ".",
            Windows.System.VirtualKey.Divide => "/",
            // OEM punctuation keys
            (Windows.System.VirtualKey)186 => shiftPressed ? ":" : ";",   // Semicolon / Colon
            (Windows.System.VirtualKey)187 => shiftPressed ? "+" : "=",   // Equal / Plus
            (Windows.System.VirtualKey)188 => shiftPressed ? "<" : ",",   // Comma / Less-than
            (Windows.System.VirtualKey)189 => shiftPressed ? "_" : "-",   // Minus / Underscore
            (Windows.System.VirtualKey)190 => shiftPressed ? ">" : ".",   // Period / Greater-than
            (Windows.System.VirtualKey)191 => shiftPressed ? "?" : "/",   // Slash / Question
            (Windows.System.VirtualKey)192 => shiftPressed ? "~" : "`",   // Backtick / Tilde
            (Windows.System.VirtualKey)219 => shiftPressed ? "{" : "[",   // Open bracket / brace
            (Windows.System.VirtualKey)220 => shiftPressed ? "|" : "\\",  // Backslash / Pipe
            (Windows.System.VirtualKey)221 => shiftPressed ? "}" : "]",   // Close bracket / brace
            (Windows.System.VirtualKey)222 => shiftPressed ? "\"" : "'",  // Quote / Double-quote
            _ => null
        };
    }

    private static string ShiftedDigit(int digit)
    {
        return digit switch
        {
            0 => ")",
            1 => "!",
            2 => "@",
            3 => "#",
            4 => "$",
            5 => "%",
            6 => "^",
            7 => "&",
            8 => "*",
            9 => "(",
            _ => string.Empty
        };
    }
}
