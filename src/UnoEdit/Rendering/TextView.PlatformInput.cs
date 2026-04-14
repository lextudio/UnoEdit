using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Xaml;
#if WINDOWS_APP_SDK
using Windows.UI.Text.Core;
#else
using LeXtudio.UI.Text.Core;
using Uno.UI.Xaml;
#endif
using Windows.Foundation;
using UnoEdit.Logging;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{

    // Composition state tracked in the document.
    private bool _isComposing;
    private int _compositionStartOffset;
    private int _compositionLength;

#if WINDOWS_APP_SDK
    // -----------------------------------------------------------------------
    // WinUI 3: native Windows.UI.Text.Core integration
    // -----------------------------------------------------------------------

    private CoreTextEditContext? _coreTextEditContext;
    /// <summary>
    /// Offset delta to correct CoreText's unreliable internal position tracking.
    /// Computed at the first TextUpdating of each composition as (our cursor – CoreText's range start).
    /// Applied to all subsequent CoreText ranges/selections during that composition.
    /// </summary>
    private int _coreTextOffsetDelta;
    private bool _coreTextDeltaEstablished;

    partial void InitializePlatformInputBridge()
    {
        Loaded += OnPlatformInputLoaded;
        Unloaded += OnPlatformInputUnloaded;
        GotFocus += OnPlatformInputGotFocus;
        LostFocus += OnPlatformInputLostFocus;
    }

    partial void UpdatePlatformInputBridge()
    {
        if (_coreTextEditContext is null)
        {
            return;
        }

        try
        {
            _coreTextEditContext.NotifyLayoutChanged();
            _coreTextEditContext.NotifySelectionChanged(ToCoreTextRange(SelectionStartOffset, SelectionEndOffset));
        }
        catch (Exception ex)
        {
            PlatformImeLogger.Log($"CoreText notify failed: {ex.Message}");
        }
    }

    partial void FocusPlatformInputBridge()
    {
        if (_coreTextEditContext is not null)
        {
            try
            {
                _coreTextEditContext.NotifyFocusEnter();
            }
            catch (Exception ex)
            {
                PlatformImeLogger.Log($"CoreText NotifyFocusEnter failed: {ex.Message}");
            }
        }
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        return false;
    }

    private void OnPlatformInputLoaded(object sender, RoutedEventArgs e)
    {
        EnsureCoreTextEditContext();
        UpdatePlatformInputBridge();
    }

    private void OnPlatformInputUnloaded(object sender, RoutedEventArgs e)
    {
        DisposeCoreTextEditContext();
    }

    private void OnPlatformInputGotFocus(object sender, RoutedEventArgs e)
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

    private void OnPlatformInputLostFocus(object sender, RoutedEventArgs e)
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
            _coreTextEditContext.SelectionRequested += CoreTextEditContext_SelectionRequested;
            _coreTextEditContext.TextUpdating += CoreTextEditContext_TextUpdating;
            _coreTextEditContext.SelectionUpdating += CoreTextEditContext_SelectionUpdating;
            _coreTextEditContext.LayoutRequested += CoreTextEditContext_LayoutRequested;
            _coreTextEditContext.CompositionStarted += CoreTextEditContext_CompositionStarted;
            _coreTextEditContext.CompositionCompleted += CoreTextEditContext_CompositionCompleted;
            _coreTextEditContext.FocusRemoved += CoreTextEditContext_FocusRemoved;

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

    private void DisposeCoreTextEditContext()
    {
        if (_coreTextEditContext is null)
        {
            return;
        }

        _coreTextEditContext.TextRequested -= CoreTextEditContext_TextRequested;
        _coreTextEditContext.SelectionRequested -= CoreTextEditContext_SelectionRequested;
        _coreTextEditContext.TextUpdating -= CoreTextEditContext_TextUpdating;
        _coreTextEditContext.SelectionUpdating -= CoreTextEditContext_SelectionUpdating;
        _coreTextEditContext.LayoutRequested -= CoreTextEditContext_LayoutRequested;
        _coreTextEditContext.CompositionStarted -= CoreTextEditContext_CompositionStarted;
        _coreTextEditContext.CompositionCompleted -= CoreTextEditContext_CompositionCompleted;
        _coreTextEditContext.FocusRemoved -= CoreTextEditContext_FocusRemoved;
        _coreTextEditContext = null;
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

    private void CoreTextEditContext_SelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
    {
        args.Request.Selection = ToCoreTextRange(SelectionStartOffset, SelectionEndOffset);
        PlatformImeLogger.Log($"CoreText SelectionRequested sel=[{SelectionStartOffset},{SelectionEndOffset}] current={CurrentOffset}");
    }

    private void CoreTextEditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
    {
        if (_document is null || IsReadOnly)
        {
            return;
        }

        int textLength = _document.TextLength;

        // --- Delta correction --------------------------------------------------
        // WinUI 3's NotifySelectionChanged does not reliably update CoreText's
        // internal position tracking.  When the user navigates between compositions,
        // CoreText's first TextUpdating range can be off by an arbitrary amount.
        // We detect this at the start of each composition and apply a correction.
        int rawStart = args.Range.StartCaretPosition;
        int rawEnd = args.Range.EndCaretPosition;
        int rawNewSelStart = args.NewSelection.StartCaretPosition;
        int rawNewSelEnd = args.NewSelection.EndCaretPosition;

        if (_isComposing && !_coreTextDeltaEstablished)
        {
            _coreTextOffsetDelta = _compositionStartOffset - rawStart;
            _coreTextDeltaEstablished = true;
            if (_coreTextOffsetDelta != 0)
            {
                PlatformImeLogger.Log(
                    $"CoreText TextUpdating DELTA established: coreTextStart={rawStart} editorCompStart={_compositionStartOffset} delta={_coreTextOffsetDelta}");
            }
        }

        int start = Math.Clamp(rawStart + _coreTextOffsetDelta, 0, textLength);
        int end = Math.Clamp(rawEnd + _coreTextOffsetDelta, start, textLength);
        int removeLength = end - start;
        string inText = args.Text ?? string.Empty;
        PlatformImeLogger.Log(
            $"CoreText TextUpdating range=[{rawStart},{rawEnd})+delta({_coreTextOffsetDelta})=[{start},{end}) remove={removeLength} " +
            $"newText='{inText}'(len={inText.Length}) newSel=[{rawNewSelStart},{rawNewSelEnd}]+delta=[{rawNewSelStart + _coreTextOffsetDelta},{rawNewSelEnd + _coreTextOffsetDelta}] " +
            $"BEFORE: docLen={textLength} current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset} " +
            $"composing={_isComposing} compStart={_compositionStartOffset} compLen={_compositionLength}");

        if (!CanDelete(start, removeLength))
        {
            return;
        }

        string text = args.Text ?? string.Empty;
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
            int newSelStart = Math.Clamp(rawNewSelStart + _coreTextOffsetDelta, 0, newTextLength);
            int newSelEnd = Math.Clamp(rawNewSelEnd + _coreTextOffsetDelta, 0, newTextLength);
            _selectionAnchorOffset = newSelStart;
            SelectionStartOffset = Math.Min(newSelStart, newSelEnd);
            SelectionEndOffset = Math.Max(newSelStart, newSelEnd);
            CurrentOffset = newSelEnd;
            _desiredColumn = _document.GetLocation(CurrentOffset).Column;
        });

        PlatformImeLogger.Log(
            $"CoreText TextUpdating AFTER: docLen={_document.TextLength} current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset}");

        if (!string.IsNullOrEmpty(text))
        {
            RaiseTextEntered(text);
        }

        UpdatePlatformInputBridge();
    }

    private void CoreTextEditContext_SelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
    {
        if (_document is null)
        {
            return;
        }

        int textLength = _document.TextLength;
        int start = Math.Clamp(args.Selection.StartCaretPosition + _coreTextOffsetDelta, 0, textLength);
        int end = Math.Clamp(args.Selection.EndCaretPosition + _coreTextOffsetDelta, 0, textLength);
        PlatformImeLogger.Log($"CoreText SelectionUpdating raw=[{args.Selection.StartCaretPosition},{args.Selection.EndCaretPosition}]+delta({_coreTextOffsetDelta})=[{start},{end}] docLen={textLength}");

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
        var request = args.Request;
        if (request is null)
        {
            return;
        }

        Rect caretRect = CalculatePlatformInputCaretRect();
        Rect controlRect = GetElementRectInWindow(RootBorder);
        Rect screenCaretRect = caretRect;
        Rect screenControlRect = controlRect;
        nint hwnd = TryGetNativeWindowHandle(Window.Current);
        if (hwnd != nint.Zero && TryGetClientOriginInScreen(hwnd, out Point clientOrigin))
        {
            screenCaretRect = OffsetRect(caretRect, clientOrigin.X, clientOrigin.Y);
            screenControlRect = OffsetRect(controlRect, clientOrigin.X, clientOrigin.Y);
        }

        request.LayoutBounds.TextBounds = screenCaretRect;
        request.LayoutBounds.ControlBounds = screenControlRect;

        PlatformImeLogger.Log(
            $"CoreText LayoutRequested clientCaret=({caretRect.X:F1},{caretRect.Y:F1},{caretRect.Width:F1},{caretRect.Height:F1}) " +
            $"clientControl=({controlRect.X:F1},{controlRect.Y:F1},{controlRect.Width:F1},{controlRect.Height:F1}) " +
            $"screenCaret=({screenCaretRect.X:F1},{screenCaretRect.Y:F1},{screenCaretRect.Width:F1},{screenCaretRect.Height:F1}) " +
            $"screenControl=({screenControlRect.X:F1},{screenControlRect.Y:F1},{screenControlRect.Width:F1},{screenControlRect.Height:F1}) hwnd=0x{hwnd:X}");
    }

    private void CoreTextEditContext_CompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
    {
        PlatformImeLogger.Log(
            $"CoreText CompositionStarted BEFORE: current={CurrentOffset} selStart={SelectionStartOffset} selEnd={SelectionEndOffset} " +
            $"composing={_isComposing} compStart={_compositionStartOffset} compLen={_compositionLength} docLen={_document?.TextLength ?? -1}");
        // Reset delta tracking — will be established on the first TextUpdating of this composition.
        _coreTextDeltaEstablished = false;
        _coreTextOffsetDelta = 0;
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

    private void CoreTextEditContext_FocusRemoved(CoreTextEditContext sender, object args)
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

    private static CoreTextRange ToCoreTextRange(int startOffset, int endOffset)
    {
        return new CoreTextRange
        {
            StartCaretPosition = Math.Min(startOffset, endOffset),
            EndCaretPosition = Math.Max(startOffset, endOffset),
        };
    }

    private static Rect OffsetRect(Rect rect, double offsetX, double offsetY)
    {
        return new Rect(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern bool ClientToScreen(nint hWnd, ref NativePoint lpPoint);

    private static bool TryGetClientOriginInScreen(nint hwnd, out Point point)
    {
        point = default;
        var nativePoint = new NativePoint { X = 0, Y = 0 };
        if (!ClientToScreen(hwnd, ref nativePoint))
        {
            return false;
        }

        point = new Point(nativePoint.X, nativePoint.Y);
        return true;
    }

    private static Rect GetElementRectInWindow(FrameworkElement element)
    {
        GeneralTransform transform = element.TransformToVisual(null);
        Point point = transform.TransformPoint(new Point(0, 0));
        return new Rect(point.X, point.Y, element.ActualWidth, element.ActualHeight);
    }

#else
    // -----------------------------------------------------------------------
    // Uno Platform / Skia Desktop: LeXtudio.UI.Text.Core shim integration
    // -----------------------------------------------------------------------

    private CoreTextEditContext? _platformTextContext;

    partial void InitializePlatformInputBridge()
    {
        Loaded += OnPlatformInputLoaded;
        Unloaded += OnPlatformInputUnloaded;
        GotFocus += OnPlatformInputGotFocus;
        LostFocus += OnPlatformInputLostFocus;
    }

    partial void UpdatePlatformInputBridge()
    {
        if (_platformTextContext is null)
        {
            return;
        }

        Rect rect = CalculatePlatformInputCaretRect();
        double scale = RootBorder.XamlRoot?.RasterizationScale ?? 1.0;
        _platformTextContext.NotifyCaretRectChanged(rect.X, rect.Y, rect.Width, rect.Height, scale);
    }

    partial void FocusPlatformInputBridge()
    {
        _platformTextContext?.NotifyFocusEnter();
        UpdatePlatformInputBridge();
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        // Linux: IME integration is via explicit TryForwardKeyToPlatformIme calls
        // in OnRootKeyDown, NOT via full deferral. Non-IME keys must fall through
        // to EditingCommandHandler / CaretNavigationCommandHandler.
        // Only macOS uses full deferral (all keys go through the native AppKit bridge).
        return false;
    }

    /// <summary>
    /// Forwards a key event to the platform IME (IBus on Linux).
    /// Returns true if the IME consumed the key (caller should suppress normal handling).
    /// </summary>
    private bool TryForwardKeyToPlatformIme(Windows.System.VirtualKey key, bool controlPressed, bool shiftPressed, char? unicodeKey = null)
    {
        if (_platformTextContext is null)
        {
            return false;
        }

        bool handled = _platformTextContext.ProcessKeyEvent((int)key, shiftPressed, controlPressed, unicodeKey);
        PlatformImeLogger.Log($"ProcessKeyEvent key={key} shift={shiftPressed} ctrl={controlPressed} -> handled={handled}");
        return handled;
    }

    private void OnPlatformInputLoaded(object sender, RoutedEventArgs e)
    {
        EnsurePlatformTextContext();
        UpdatePlatformInputBridge();
    }

    private void OnPlatformInputUnloaded(object sender, RoutedEventArgs e)
    {
        DisposePlatformTextContext();
    }

    private void OnPlatformInputGotFocus(object sender, RoutedEventArgs e)
    {
        _platformTextContext?.NotifyFocusEnter();
    }

    private void OnPlatformInputLostFocus(object sender, RoutedEventArgs e)
    {
        _platformTextContext?.NotifyFocusLeave();
    }

    private void EnsurePlatformTextContext()
    {
        if (_platformTextContext is not null)
        {
            return;
        }

        nint windowHandle = nint.Zero;
        nint displayHandle = nint.Zero;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            TryGetX11Handles(out displayHandle, out windowHandle);
        }

        if (windowHandle == nint.Zero)
        {
            windowHandle = TryGetNativeWindowHandle(Window.Current);
        }

        CoreTextServicesManager manager = CoreTextServicesManager.GetForCurrentView();
        _platformTextContext = manager.CreateEditContext();

        _platformTextContext.TextRequested += OnPlatformTextRequested;
        _platformTextContext.TextUpdating += OnPlatformTextUpdating;
        _platformTextContext.CompositionStarted += OnPlatformCompositionStarted;
        _platformTextContext.CompositionCompleted += OnPlatformCompositionCompleted;
        _platformTextContext.FocusRemoved += OnPlatformFocusRemoved;
        _platformTextContext.CommandReceived += OnPlatformCommandReceived;

        bool attached = _platformTextContext.Attach(windowHandle, displayHandle);
        PlatformImeLogger.Log($"Attach platform text context handle=0x{windowHandle:X} display=0x{displayHandle:X} attached={attached}");

        // The editor may already have focus when Loaded fires, so GotFocus
        // won't re-fire.  Ensure IBus knows we have focus.
        if (attached)
        {
            _platformTextContext.NotifyFocusEnter();
        }
    }

    /// <summary>
    /// Resolve the X11 display and window handles from Uno internals via reflection.
    /// Path: X11XamlRootHost.GetHostFromWindow(Window.Current) → .RootX11Window → .Display/.Window
    /// </summary>
    private static void TryGetX11Handles(out nint display, out nint window)
    {
        display = nint.Zero;
        window = nint.Zero;

        try
        {
            var currentWindow = Window.Current;
            if (currentWindow is null) return;

            var hostType = Type.GetType("Uno.WinUI.Runtime.Skia.X11.X11XamlRootHost, Uno.UI.Runtime.Skia.X11");
            if (hostType is null) return;

            var getHost = hostType.GetMethod("GetHostFromWindow", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var host = getHost?.Invoke(null, new object?[] { currentWindow });
            if (host is null) return;

            var rootX11WindowProp = hostType.GetProperty("RootX11Window", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var x11Window = rootX11WindowProp?.GetValue(host);
            if (x11Window is null) return;

            var windowType = x11Window.GetType();
            var displayProp = windowType.GetProperty("Display", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var windowProp = windowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            object? displayVal = displayProp?.GetValue(x11Window);
            if (displayVal is nint dp) display = dp;
            else if (displayVal is IntPtr dip) display = dip;

            object? windowVal = windowProp?.GetValue(x11Window);
            if (windowVal is nint wp) window = wp;
            else if (windowVal is IntPtr wip) window = wip;
        }
        catch
        {
        }
    }

    private void DisposePlatformTextContext()
    {
        if (_platformTextContext is null)
        {
            return;
        }

        _platformTextContext.TextRequested -= OnPlatformTextRequested;
        _platformTextContext.TextUpdating -= OnPlatformTextUpdating;
        _platformTextContext.CompositionStarted -= OnPlatformCompositionStarted;
        _platformTextContext.CompositionCompleted -= OnPlatformCompositionCompleted;
        _platformTextContext.FocusRemoved -= OnPlatformFocusRemoved;
        _platformTextContext.CommandReceived -= OnPlatformCommandReceived;
        _platformTextContext.Dispose();
        _platformTextContext = null;
    }

    private void OnPlatformTextRequested(object? sender, CoreTextTextRequestedEventArgs e)
    {
        string text = e.Request.Text ?? string.Empty;
        PlatformImeLogger.Log($"OnPlatformTextRequested: text='{text}'");
        if (!string.IsNullOrEmpty(text))
        {
            HandleCompositionCommit(text);
        }
    }

    private void OnPlatformTextUpdating(object? sender, CoreTextTextUpdatingEventArgs e)
    {
        PlatformImeLogger.Log($"OnPlatformTextUpdating: text='{e.NewText}'");
        HandleCompositionUpdate(e.NewText ?? string.Empty);
    }

    private void OnPlatformCompositionStarted(object? sender, EventArgs e)
    {
        PlatformImeLogger.Log("OnPlatformCompositionStarted");
        HandleCompositionStart();
    }

    private void OnPlatformCompositionCompleted(object? sender, EventArgs e)
    {
        HandleCompositionEnd();
    }

    private void OnPlatformFocusRemoved(object? sender, EventArgs e)
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

    private void OnPlatformCommandReceived(object? sender, CoreTextCommandReceivedEventArgs e)
    {
        PlatformImeLogger.Log($"CommandReceived: {e.Command}");
        bool handled = e.Command switch
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
            e.Handled = true;
            return;
        }

        if (e.Command == "paste:")
        {
            _ = PasteAsync();
            e.Handled = true;
        }
    }
#endif

    // -----------------------------------------------------------------------
    // Composition document management (shared)
    // -----------------------------------------------------------------------

    internal void HandleCompositionStart()
    {
        _compositionStartOffset = CurrentOffset;
        _compositionLength = 0;
        _isComposing = true;
        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionUpdate(string text)
    {
        if (_document is null)
        {
            return;
        }

        if (!_isComposing)
        {
            HandleCompositionStart();
        }

        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                if (_compositionLength > 0)
                {
                    _document.Remove(_compositionStartOffset, _compositionLength);
                }

                if (text.Length > 0)
                {
                    _document.Insert(_compositionStartOffset, text);
                }

                _compositionLength = text.Length;
            }

            CollapseSelection(_compositionStartOffset + text.Length);
        });

        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionEnd()
    {
        if (_document is null || !_isComposing)
        {
            _isComposing = false;
            _compositionLength = 0;
            return;
        }

        BatchRefresh(() =>
        {
            if (_compositionLength > 0)
            {
                using (_document.RunUpdate())
                {
                    _document.Remove(_compositionStartOffset, _compositionLength);
                }

                _compositionLength = 0;
                CollapseSelection(_compositionStartOffset);
            }

            _isComposing = false;
        });

        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionCommit(string text)
    {
        if (_document is null)
        {
            return;
        }

        int insertAt = _isComposing ? _compositionStartOffset : CurrentOffset;
        int removeLen = _isComposing ? _compositionLength : 0;

        RaiseTextEntering(text);

        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                if (removeLen > 0)
                {
                    _document.Remove(insertAt, removeLen);
                }

                _document.Insert(insertAt, text);
            }

            _isComposing = false;
            _compositionLength = 0;
            CollapseSelection(insertAt + text.Length);
        });

        RaiseTextEntered(text);
        UpdatePlatformInputBridge();
    }

    // -----------------------------------------------------------------------
    // Shared: caret rect and native window handle
    // -----------------------------------------------------------------------

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

    private static nint TryGetNativeWindowHandle(Window? window)
    {
        if (window is null)
        {
            // Window.Current can be null in WinUI 3 unpackaged apps and in Uno Skia
            // Desktop; fall back to the process main window handle so Win32 IME
            // subclassing can proceed.
            return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        }

#if WINDOWS_APP_SDK
        try
        {
            return (nint)WinRT.Interop.WindowNative.GetWindowHandle(window);
        }
        catch
        {
            return nint.Zero;
        }
#else
        object? nativeWindow = WindowHelper.GetNativeWindow(window);
        if (nativeWindow is null)
        {
            return nint.Zero;
        }

        string nativeTypeName = nativeWindow.GetType().FullName ?? string.Empty;

        if (nativeTypeName == "System.Windows.Window")
        {
            try
            {
                Type? helperType = Type.GetType(
                    "System.Windows.Interop.WindowInteropHelper, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                helperType ??= Type.GetType("System.Windows.Interop.WindowInteropHelper, PresentationFramework");
                if (helperType is null)
                {
                    return nint.Zero;
                }

                object? helper = Activator.CreateInstance(helperType, nativeWindow);
                PropertyInfo? handleProp = helperType.GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public);

                if (handleProp?.GetValue(helper) is IntPtr hwndIntPtr)
                {
                    return hwndIntPtr;
                }
            }
            catch
            {
            }

            return nint.Zero;
        }

        // Uno Skia Desktop (newer Uno): native window is Uno.UI... Win32NativeWindow.
        // Reflect over known property/field names to find the HWND.
        if (nativeTypeName.Contains("Win32NativeWindow", StringComparison.Ordinal))
        {
            foreach (string name in new[] { "Hwnd", "HWnd", "Handle", "WindowHandle", "NativeHandle", "Pointer", "hwnd", "_hwnd" })
            {
                PropertyInfo? p = nativeWindow.GetType().GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p is not null)
                {
                    object? v = p.GetValue(nativeWindow);
                    if (v is nint np && np != nint.Zero) return np;
                    if (v is IntPtr ip && ip != IntPtr.Zero) return ip;
                    if (v is long lv && lv != 0) return new nint(lv);
                }

                FieldInfo? f = nativeWindow.GetType().GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f is not null)
                {
                    object? v = f.GetValue(nativeWindow);
                    if (v is nint np && np != nint.Zero) return np;
                    if (v is IntPtr ip && ip != IntPtr.Zero) return ip;
                    if (v is long lv && lv != 0) return new nint(lv);
                }
            }

            return nint.Zero;
        }

        PropertyInfo? handleProperty = nativeWindow.GetType().GetProperty(
            "Handle",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (handleProperty?.GetValue(nativeWindow) is nint handle)
        {
            return handle;
        }

        if (handleProperty?.GetValue(nativeWindow) is IntPtr intPtrHandle)
        {
            return intPtrHandle;
        }

        if (handleProperty?.GetValue(nativeWindow) is long longHandle)
        {
            return new nint(longHandle);
        }

        return nint.Zero;
#endif
    }
}
