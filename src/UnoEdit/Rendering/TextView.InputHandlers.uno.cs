using System.Reflection;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UnoEdit.Logging;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{
    /// <summary>
    /// Cached reflection accessor for KeyRoutedEventArgs.UnicodeKey (internal).
    /// Uno stores the printable character from XLookupString here, even when
    /// <see cref="Windows.System.VirtualKey"/> is <c>None</c> (OEM punctuation keys on Skia/Linux).
    /// </summary>
    private static readonly PropertyInfo? s_unicodeKeyProperty =
        typeof(KeyRoutedEventArgs).GetProperty("UnicodeKey", BindingFlags.Instance | BindingFlags.NonPublic);

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = SelectionMouseHandler.HandlePointerPressed(this, e);
    }

    private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = SelectionMouseHandler.HandlePointerMoved(this, e);
    }

    private void OnRootPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = SelectionMouseHandler.HandlePointerReleased(this, e);
    }

    private void OnFoldGlyphPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = SelectionMouseHandler.HandleFoldGlyphPointerPressed(this, sender);
    }

    private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        bool extendSelection = IsShiftPressed();
        bool controlPressed = IsControlPressed();

        // macOS: defer all key handling to the native AppKit bridge.
        if (ShouldDeferToPlatformTextInput(controlPressed))
        {
            PlatformImeLogger.Log($"KeyDown deferred to native bridge. Key={e.Key}, controlPressed={controlPressed}, shiftPressed={extendSelection}");
            e.Handled = true;
            return;
        }

        // Uno on Skia/Linux fires VirtualKey.None for OEM punctuation keys (; / [ ] \ ' ` etc.)
        // but stores the real character from XLookupString in the internal UnicodeKey property.
        // Recover it here so the character can be forwarded to IBus and/or inserted.
        char? unicodeKey = s_unicodeKeyProperty?.GetValue(e) as char?;

#if !WINDOWS_APP_SDK
        // Linux: forward the key to IBus synchronously before UnoEdit processes it.
        // If IBus handles the key (e.g. for IME composition), suppress normal processing.
        if (TryForwardKeyToPlatformIme(e.Key, controlPressed, extendSelection, unicodeKey))
        {
            e.Handled = true;
            return;
        }
#endif
        // For VirtualKey.None with a printable UnicodeKey, insert the character directly.
        if (e.Key == Windows.System.VirtualKey.None && unicodeKey.HasValue && !char.IsControl(unicodeKey.Value) && !controlPressed)
        {
            if (!_isComposing)
            {
                InsertText(unicodeKey.Value.ToString(), raiseTextInputEvents: true);
            }
            e.Handled = true;
            return;
        }

        bool handled = await EditingCommandHandler.HandleKeyDownAsync(this, e.Key, controlPressed, extendSelection);
        if (!handled)
        {
            handled = CaretNavigationCommandHandler.HandleKeyDown(this, e.Key, controlPressed, extendSelection);
        }

        e.Handled = handled;
    }

    internal bool HandlePointerPressedCore(PointerRoutedEventArgs e)
    {
        if (_document is null)
        {
            return false;
        }

        Focus(FocusState.Programmatic);
        FocusPlatformInputBridge();

        var point = e.GetCurrentPoint(ContentStackPanel).Position;
        if (TryToggleFoldAtViewportPoint(point.X, point.Y))
        {
            return true;
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
                return true;
            }
        }

        _isPointerSelecting = true;
        CapturePointer(e.Pointer);

        UpdateCaretAndSelection(targetOffset, extendSelection);
        if (!extendSelection)
        {
            _selectionAnchorOffset = targetOffset;
        }

        return true;
    }

    internal bool HandlePointerMovedCore(PointerRoutedEventArgs e)
    {
        if (_document is null || !_isPointerSelecting)
        {
            return false;
        }

        // Uno Skia can occasionally lose capture on focus/window transitions.
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            _isPointerSelecting = false;
            ReleasePointerCapture(e.Pointer);
            return false;
        }

        var point = e.GetCurrentPoint(ContentStackPanel).Position;
        int targetOffset = GetOffsetFromViewPoint(point.X, point.Y);

        BatchRefresh(() =>
        {
            CurrentOffset = targetOffset;
            SelectionStartOffset = Math.Min(_selectionAnchorOffset, targetOffset);
            SelectionEndOffset = Math.Max(_selectionAnchorOffset, targetOffset);
        });
        return true;
    }

    internal bool HandlePointerReleasedCore(PointerRoutedEventArgs e)
    {
        if (!_isPointerSelecting)
        {
            return false;
        }

        _isPointerSelecting = false;
        ReleasePointerCapture(e.Pointer);
        return true;
    }

    internal bool HandleFoldGlyphPointerPressedCore(object sender)
    {
        if (_document is null)
        {
            return false;
        }

        if (sender is not FrameworkElement { DataContext: TextLineViewModel lineViewModel })
        {
            return false;
        }

        if (!int.TryParse(lineViewModel.Number, out int lineNumber))
        {
            return false;
        }

        return TryToggleFoldAtDocumentLine(lineNumber);
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

    internal bool ToggleFoldAtCaret()
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
