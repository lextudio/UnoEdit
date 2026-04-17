using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Xaml;
using UnoEdit.Logging;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{

    // Composition state tracked in the document.
    private bool _isComposing;
    private int _compositionStartOffset;
    private int _compositionLength;

    private CoreTextEditContext _coreTextEditContext;

    partial void InitializePlatformInputBridge()
    {
        Loaded += CoreTextEditContext_InputLoaded;
        Unloaded += CoreTextEditContext_InputUnloaded;
        GotFocus += CoreTextEditContext_InputGotFocus;
        LostFocus += CoreTextEditContext_InputLostFocus;
    }

    partial void UpdatePlatformInputBridge()
    {
        if (_coreTextEditContext is null)
        {
            return;
        }

        try
        {
            _coreTextEditContext.SyncState(
                CalculatePlatformInputCaretRect(),
                GetElementRectInWindow(RootBorder),
                RootBorder.XamlRoot?.RasterizationScale ?? 1.0,
                SelectionStartOffset,
                SelectionEndOffset,
                Window.Current);
        }
        catch (Exception ex)
        {
            PlatformImeLogger.Log($"Platform Notify failed: {ex.Message}");
        }
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        // Linux: IME integration is via explicit TryForwardKeyToPlatformIme calls
        // in OnRootKeyDown, NOT via full deferral. Non-IME keys must fall through
        // to EditingCommandHandler / CaretNavigationCommandHandler.
        // Only macOS uses full deferral (all keys go through the native AppKit bridge).
        return false;
    }

    private void CoreTextEditContext_InputLoaded(object sender, RoutedEventArgs e)
    {
        EnsureCoreTextEditContext();
        UpdatePlatformInputBridge();
    }

    private void CoreTextEditContext_InputUnloaded(object sender, RoutedEventArgs e)
    {
        DisposeCoreTextEditContext();
    }

    private void CoreTextEditContext_InputGotFocus(object sender, RoutedEventArgs e)
    {
        if (_coreTextEditContext is not null)
        {
            try
            {
                _coreTextEditContext.NotifyFocusEnter();
            }
            catch (Exception ex)
            {
                PlatformImeLogger.Log($"CoreText focus enter failed: {ex.Message}");
            }
        }
    }

    private void CoreTextEditContext_InputLostFocus(object sender, RoutedEventArgs e)
    {
        if (_coreTextEditContext is not null)
        {
            try
            {
                _coreTextEditContext.NotifyFocusLeave();
            }
            catch (Exception ex)
            {
                PlatformImeLogger.Log($"CoreText focus leave failed: {ex.Message}");
            }
        }
    }

    private void DisposeCoreTextEditContext()
    {
        if (_coreTextEditContext is null)
        {
            return;
        }

        _coreTextEditContext.TextRequested -= CoreTextEditContext_TextRequested;
        _coreTextEditContext.TextUpdating -= CoreTextEditContext_TextUpdating;
        _coreTextEditContext.SelectionRequested -= CoreTextEditContext_SelectionRequested;
        _coreTextEditContext.SelectionUpdating -= CoreTextEditContext_SelectionUpdating;
        _coreTextEditContext.LayoutRequested -= CoreTextEditContext_LayoutRequested;
        _coreTextEditContext.CompositionStarted -= CoreTextEditContext_CompositionStarted;
        _coreTextEditContext.CompositionCompleted -= CoreTextEditContext_CompositionCompleted;
        _coreTextEditContext.FocusRemoved -= CoreTextEditContext_FocusRemoved;
        _coreTextEditContext.CommandReceived -= CoreTextEditContext_CommandReceived;
        _coreTextEditContext.Dispose();
        _coreTextEditContext = null;
    }

    private bool EnsureCoreTextEditContext()
    {
        if (_coreTextEditContext is not null)
        {
            return true;
        }

        try
        {
            CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
            _coreTextEditContext = manager.CreateEditContext();
            _coreTextEditContext.InputScope = CoreTextInputScope.Text;

            _coreTextEditContext.TextRequested += CoreTextEditContext_TextRequested;
            _coreTextEditContext.TextUpdating += CoreTextEditContext_TextUpdating;
            _coreTextEditContext.SelectionRequested += CoreTextEditContext_SelectionRequested;
            _coreTextEditContext.SelectionUpdating += CoreTextEditContext_SelectionUpdating;
            _coreTextEditContext.LayoutRequested += CoreTextEditContext_LayoutRequested;
            _coreTextEditContext.CompositionStarted += CoreTextEditContext_CompositionStarted;
            _coreTextEditContext.CompositionCompleted += CoreTextEditContext_CompositionCompleted;
            _coreTextEditContext.FocusRemoved += CoreTextEditContext_FocusRemoved;
            _coreTextEditContext.CommandReceived += CoreTextEditContext_CommandReceived;

            bool attached = _coreTextEditContext.AttachToCurrentWindow(Window.Current);
            if (attached)
            {
                _coreTextEditContext.NotifyFocusEnter();
            }

            PlatformImeLogger.Log("EnsureCoreTextEditContext: initialized.");
            return true;
        }
        catch (Exception ex)
        {
            PlatformImeLogger.Log($"EnsureCoreTextEditContext failed: {ex.Message}");
            _coreTextEditContext = null;
            return false;
        }
    }

    partial void FocusPlatformInputBridge()
    {
        if (_coreTextEditContext is null)
        {
            return;
        }

        try
        {
            _coreTextEditContext.NotifyFocusEnter();
            UpdatePlatformInputBridge();
        }
        catch (Exception ex)
        {
            PlatformImeLogger.Log($"CoreText NotifyFocusEnter failed: {ex.Message}");
        }
    }

    private void CoreTextEditContext_CompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
    {
        PlatformImeLogger.Log(
            $"CoreText CompositionStarted BEFORE: current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset} " +
            $"composing={_isComposing} compStart={_compositionStartOffset} compLen={_compositionLength} docLen={_document?.TextLength ?? -1}");
        HandleCompositionStart();
    }

    private void CoreTextEditContext_CompositionCompleted(CoreTextEditContext sender, CoreTextCompositionCompletedEventArgs args)
    {
        PlatformImeLogger.Log(
            $"CoreText CompositionCompleted BEFORE: current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset} " +
            $"composing={_isComposing} compStart={_compositionStartOffset} compLen={_compositionLength} docLen={_document?.TextLength ?? -1}");
        HandleCompositionEnd();
        PlatformImeLogger.Log(
            $"CoreText CompositionCompleted AFTER: current={CurrentOffset} composing={_isComposing}");
    }

    private void CoreTextEditContext_FocusRemoved(CoreTextEditContext? sender, object result)
    {
        try
        {
            if (FocusState != FocusState.Unfocused)
            {
                Focus(FocusState.Unfocused);
            }
        }
        catch
        {
        }
    }

    private void CoreTextEditContext_TextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
    {
        CoreTextTextRequest request = args.Request;
        if (_document is null)
        {
            request.Text = string.Empty;
            return;
        }

        int textLength = _document.TextLength;
        int start = Math.Clamp(request.Range.StartCaretPosition, 0, textLength);
        int end = Math.Clamp(request.Range.EndCaretPosition, start, textLength);
        request.Text = _document.GetText(start, end - start);
        PlatformImeLogger.Log($"CoreText TextRequested range=[{start},{end}) len={end - start}");
    }

    private void CoreTextEditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
    {
        if (_document is null || IsReadOnly)
        {
            return;
        }

        int textLength = _document.TextLength;
        int start = Math.Clamp(args.Range.StartCaretPosition, 0, textLength);
        int end = Math.Clamp(args.Range.EndCaretPosition, start, textLength);
        int removeLength = end - start;
        string text = args.Text ?? string.Empty;

        PlatformImeLogger.Log(
            $"CoreText TextUpdating range=[{start},{end}) remove={removeLength} " +
            $"newText='{text}'(len={text.Length}) newSel=[{args.NewSelection.StartCaretPosition},{args.NewSelection.EndCaretPosition}] " +
            $"BEFORE: docLen={textLength} current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset} " +
            $"composing={_isComposing} compStart={_compositionStartOffset} compLen={_compositionLength}");

        if (!CanDelete(start, removeLength))
        {
            return;
        }

        if (!string.IsNullOrEmpty(text))
        {
            RaiseTextEntering(text);
        }

        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                if (removeLength > 0)
                {
                    _document.Remove(start, removeLength);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    _document.Insert(start, text);
                }
            }

            int newTextLength = _document.TextLength;
            int newSelStart = Math.Clamp(args.NewSelection.StartCaretPosition, 0, newTextLength);
            int newSelEnd = Math.Clamp(args.NewSelection.EndCaretPosition, 0, newTextLength);
            _selectionAnchorOffset = newSelStart;
            SelectionStartOffset = Math.Min(newSelStart, newSelEnd);
            SelectionEndOffset = Math.Max(newSelStart, newSelEnd);
            CurrentOffset = newSelEnd;
            _desiredColumn = _document.GetLocation(CurrentOffset).Column;

            if (_isComposing)
            {
                _compositionStartOffset = start;
                _compositionLength = text.Length;
            }
        });

        PlatformImeLogger.Log(
            $"CoreText TextUpdating AFTER: docLen={_document.TextLength} current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset}");

        if (!string.IsNullOrEmpty(text))
        {
            RaiseTextEntered(text);
        }

        UpdatePlatformInputBridge();
    }

    private void CoreTextEditContext_SelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
    {
        args.Request.Selection = new CoreTextRange
        {
            StartCaretPosition = SelectionStartOffset,
            EndCaretPosition = SelectionEndOffset,
        };

        PlatformImeLogger.Log($"CoreText SelectionRequested sel=[{SelectionStartOffset},{SelectionEndOffset}] current={CurrentOffset}");
    }

    private void CoreTextEditContext_SelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
    {
        if (_document is null)
        {
            return;
        }

        int textLength = _document.TextLength;
        int start = Math.Clamp(args.Selection.StartCaretPosition, 0, textLength);
        int end = Math.Clamp(args.Selection.EndCaretPosition, 0, textLength);
        PlatformImeLogger.Log($"CoreText SelectionUpdating sel=[{start},{end}] docLen={textLength}");

        BatchRefresh(() =>
        {
            _selectionAnchorOffset = start;
            SelectionStartOffset = Math.Min(start, end);
            SelectionEndOffset = Math.Max(start, end);
            CurrentOffset = end;
            _desiredColumn = _document.GetLocation(CurrentOffset).Column;
        });

        UpdatePlatformInputBridge();
    }

    private void CoreTextEditContext_LayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
    {
        args.ApplyLayoutBoundsCompat(
            CalculatePlatformInputCaretRect(),
            GetElementRectInWindow(RootBorder),
            Window.Current);
    }

    private bool TryForwardKeyToPlatformIme(Windows.System.VirtualKey key, bool controlPressed, bool shiftPressed, char? unicodeKey = null)
    {
        if (_coreTextEditContext is null)
        {
            return false;
        }

        bool handled = _coreTextEditContext.ProcessKeyEvent((int)key, shiftPressed, controlPressed, unicodeKey);
        PlatformImeLogger.Log($"ProcessKeyEvent key={key} shift={shiftPressed} ctrl={controlPressed} -> handled={handled}");
        return handled;
    }

    private void CoreTextEditContext_CommandReceived(object sender, CoreTextCommandReceivedEventArgs args)
    {
        string command = args.Command;
        PlatformImeLogger.Log($"CommandReceived: {command}");
        bool handled = command switch
        {
            "deleteBackward:" => Backspace(),
            "deleteForward:" => Delete(),
            "insertNewline:" => InsertText(Environment.NewLine),
            "insertTab:" => InsertText("\t"),
            "insertBacktab:" => false,
            "moveLeft:" => MoveHorizontal(-1, extendSelection: false),
            "moveRight:" => MoveHorizontal(1, extendSelection: false),
            "moveUp:" => MoveVertical(-1, extendSelection: false),
            "moveDown:" => MoveVertical(1, extendSelection: false),
            "moveWordLeft:" => MoveWordBoundary(backward: true, extendSelection: false),
            "moveWordRight:" => MoveWordBoundary(backward: false, extendSelection: false),
            "moveToBeginningOfLine:" => MoveToLineBoundary(moveToStart: true, extendSelection: false),
            "moveToEndOfLine:" => MoveToLineBoundary(moveToStart: false, extendSelection: false),
            "moveToBeginningOfDocument:" => MoveToDocumentBoundary(moveToStart: true, extendSelection: false),
            "moveToEndOfDocument:" => MoveToDocumentBoundary(moveToStart: false, extendSelection: false),
            "moveLeftAndModifySelection:" => MoveHorizontal(-1, extendSelection: true),
            "moveRightAndModifySelection:" => MoveHorizontal(1, extendSelection: true),
            "moveUpAndModifySelection:" => MoveVertical(-1, extendSelection: true),
            "moveDownAndModifySelection:" => MoveVertical(1, extendSelection: true),
            "moveWordLeftAndModifySelection:" => MoveWordBoundary(backward: true, extendSelection: true),
            "moveWordRightAndModifySelection:" => MoveWordBoundary(backward: false, extendSelection: true),
            "moveToBeginningOfLineAndModifySelection:" => MoveToLineBoundary(moveToStart: true, extendSelection: true),
            "moveToEndOfLineAndModifySelection:" => MoveToLineBoundary(moveToStart: false, extendSelection: true),
            "moveToBeginningOfDocumentAndModifySelection:" => MoveToDocumentBoundary(moveToStart: true, extendSelection: true),
            "moveToEndOfDocumentAndModifySelection:" => MoveToDocumentBoundary(moveToStart: false, extendSelection: true),
            "selectAll:" => SelectAll(),
            "copy:" => CopySelection(),
            "cut:" => CutSelection(),
            "undo:" => Undo(),
            "redo:" => Redo(),
            _ => false,
        };

        if (handled)
        {
            UpdatePlatformInputBridge();
            args.Handled = true;
            return;
        }

        if (command == "paste:")
        {
            _ = PasteAsync();
            args.Handled = true;
        }
    }

    internal void HandleCompositionStart()
    {
        _compositionStartOffset = CurrentOffset;
        _compositionLength = 0;
        _isComposing = true;
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionEnd()
    {
        if (!_isComposing)
        {
            _isComposing = false;
            _compositionLength = 0;
            return;
        }

        _isComposing = false;
        _compositionLength = 0;
        RefreshViewport();
        UpdatePlatformInputBridge();
    }

    private Rect CalculatePlatformInputCaretRect()
    {
        if (_document is null || _visibleDocLines.Count == 0 || RootBorder.XamlRoot is null)
        {
            return Rect.Empty;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        DocumentLine line = _document.GetLineByNumber(location.Line);
        string lineText = _document.GetText(line);
        int logicalColumn = Math.Clamp(location.Column - 1, 0, lineText.Length);

        int visualRow = GetVisualRow(location.Line);
        if (visualRow < 0)
        {
            visualRow = 0;
        }

        double x = GutterWidth + TextLeftPadding + GetDisplayColumnX(lineText, logicalColumn) - TextScrollViewer.HorizontalOffset;
        double y = (visualRow * LineHeight) - TextScrollViewer.VerticalOffset;

        GeneralTransform transform = RootBorder.TransformToVisual(null);
        Point point = transform.TransformPoint(new Point(x, y));
        return new Rect(point.X, point.Y + 3d, 2d, 16d);
    }

    private static Rect GetElementRectInWindow(FrameworkElement element)
    {
        GeneralTransform transform = element.TransformToVisual(null);
        Point point = transform.TransformPoint(new Point(0, 0));
        return new Rect(point.X, point.Y, element.ActualWidth, element.ActualHeight);
    }
}
