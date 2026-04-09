using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{
    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        Focus(FocusState.Programmatic);
        FocusPlatformInputBridge();

        var point = e.GetCurrentPoint(ContentStackPanel).Position;
        if (TryToggleFoldAtViewportPoint(point.X, point.Y))
        {
            e.Handled = true;
            return;
        }

        int targetOffset = GetOffsetFromViewPoint(point.X, point.Y);
        bool extendSelection = IsShiftPressed();
        bool ctrlClick = IsControlPressed();

        if (ctrlClick && ReferenceSegmentSource is { } source)
        {
            var segment = source.GetSegments(targetOffset, targetOffset + 1)
                .FirstOrDefault(candidate => candidate.Contains(targetOffset));
            if (segment is not null)
            {
                NavigationRequested?.Invoke(this, segment);
                e.Handled = true;
                return;
            }
        }

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

        // Uno Skia can occasionally lose capture on focus/window transitions.
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _isPointerSelecting = false;
            ReleasePointerCapture(e.Pointer);
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

    private void OnFoldGlyphPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: TextLineViewModel lineViewModel })
        {
            return;
        }

        if (!int.TryParse(lineViewModel.Number, out int lineNumber))
        {
            return;
        }

        if (TryToggleFoldAtDocumentLine(lineNumber))
        {
            e.Handled = true;
        }
    }

    private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        bool extendSelection = IsShiftPressed();
        bool controlPressed = IsControlPressed();

        if (controlPressed && e.Key == Windows.System.VirtualKey.M)
        {
            e.Handled = ToggleFoldAtCaret();
            return;
        }

        if (ShouldDeferToPlatformTextInput(controlPressed))
        {
            LogMacIme($"KeyDown deferred to native bridge. Key={e.Key}, controlPressed={controlPressed}, shiftPressed={extendSelection}");
            e.Handled = true;
            return;
        }

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
            _ when !controlPressed => InsertPrintableKey(e.Key, extendSelection),
            _ => false
        };

        if (!handled && controlPressed && e.Key == Windows.System.VirtualKey.V)
        {
            handled = await PasteAsync();
        }

        e.Handled = handled;
    }

    private bool TryToggleFoldAtViewportPoint(double x, double y)
    {
        if (_document is null || FoldingManager is null || x >= GutterWidth || _visibleDocLines.Count == 0)
        {
            return false;
        }

        double absoluteY = y + TextScrollViewer.VerticalOffset;
        int visualRow = Math.Clamp((int)(absoluteY / LineHeight), 0, _visibleDocLines.Count - 1);
        return TryToggleFoldAtDocumentLine(_visibleDocLines[visualRow]);
    }

    private bool TryToggleFoldAtDocumentLine(int lineNumber)
    {
        if (_document is null)
        {
            return false;
        }

        DocumentLine line = _document.GetLineByNumber(lineNumber);
        return TryToggleFoldForLine(line);
    }

    private bool ToggleFoldAtCaret()
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        DocumentLine caretLine = _document.GetLineByNumber(location.Line);
        return TryToggleFoldForLine(caretLine);
    }

    private bool TryToggleFoldForLine(DocumentLine line)
    {
        var foldingManager = FoldingManager;
        if (foldingManager is null || _document is null)
        {
            return false;
        }

        foreach (var section in foldingManager.AllFoldings)
        {
            if (section.StartOffset >= line.Offset && section.StartOffset <= line.EndOffset)
            {
                section.IsFolded = !section.IsFolded;
                AnnounceFoldState(section);
                return true;
            }
        }

        return false;
    }

    private void AnnounceFoldState(ICSharpCode.AvalonEdit.Folding.FoldingSection section)
    {
        if (_document is null || LiveRegionAnnouncer is null)
        {
            return;
        }

        int foldStartLine = _document.GetLineByOffset(section.StartOffset).LineNumber;
        int foldEndLine = _document.GetLineByOffset(section.EndOffset).LineNumber;
        LiveRegionAnnouncer.Text = section.IsFolded
            ? $"Collapsed lines {foldStartLine} to {foldEndLine}"
            : $"Expanded lines {foldStartLine} to {foldEndLine}";
    }

    private static bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    // On macOS the Command key is reported as LeftWindows / RightWindows.
    // Accept either Ctrl or Cmd so that standard shortcuts work across desktop hosts.
    private static bool IsControlPressed()
    {
        var flags = Windows.UI.Core.CoreVirtualKeyStates.Down;
        return InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftWindows).HasFlag(flags)
            || InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightWindows).HasFlag(flags);
    }
}
