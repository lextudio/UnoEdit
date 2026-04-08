using System.Reflection;
using System.Runtime.InteropServices;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{
#if __UNO_SKIA_MACOS__
    private const string ExperimentalMacImeEnvVar = "UNOEDIT_ENABLE_EXPERIMENTAL_MACOS_IME";
    private const string ExperimentalMacImeDebugEnvVar = "UNOEDIT_DEBUG_MACOS_IME";
    private MacOSNativeImeBridge? _macOSNativeImeBridge;

    private static bool IsExperimentalMacImeEnabled()
    {
        string? value = Environment.GetEnvironmentVariable(ExperimentalMacImeEnvVar);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExperimentalMacImeDebugEnabled()
    {
        string? value = Environment.GetEnvironmentVariable(ExperimentalMacImeDebugEnvVar);
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogMacIme(string message)
    {
        if (IsExperimentalMacImeDebugEnabled())
        {
            Console.WriteLine($"[UnoEdit macOS IME] {message}");
        }
    }

    partial void InitializePlatformInputBridge()
    {
        if (!IsExperimentalMacImeEnabled())
        {
            LogMacIme($"Experimental bridge disabled. Set {ExperimentalMacImeEnvVar}=1 to enable.");
            return;
        }

        LogMacIme("Initializing experimental macOS IME bridge.");
        Loaded += OnPlatformInputLoaded;
        Unloaded += OnPlatformInputUnloaded;
    }

    partial void UpdatePlatformInputBridge()
    {
        if (!IsExperimentalMacImeEnabled())
        {
            return;
        }

        _macOSNativeImeBridge?.UpdateCaretRect(CalculatePlatformInputCaretRect());
    }

    partial void FocusPlatformInputBridge()
    {
        if (!IsExperimentalMacImeEnabled())
        {
            return;
        }

        EnsureMacOSNativeImeBridge();
        LogMacIme($"Requesting native focus. Bridge available={_macOSNativeImeBridge is { IsAvailable: true }}");
        _macOSNativeImeBridge?.Focus();
        UpdatePlatformInputBridge();
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        bool shouldDefer = IsExperimentalMacImeEnabled() && !controlPressed && _macOSNativeImeBridge?.IsFocused == true;
        if (shouldDefer)
        {
            LogMacIme("Managed key handling deferred to native AppKit responder.");
        }

        return shouldDefer;
    }

    private void OnPlatformInputLoaded(object sender, RoutedEventArgs e)
    {
        LogMacIme("TextView loaded.");
        EnsureMacOSNativeImeBridge();
        UpdatePlatformInputBridge();
    }

    private void OnPlatformInputUnloaded(object sender, RoutedEventArgs e)
    {
        LogMacIme("TextView unloaded. Disposing native bridge.");
        _macOSNativeImeBridge?.Dispose();
        _macOSNativeImeBridge = null;
    }

    private void EnsureMacOSNativeImeBridge()
    {
        if (!IsExperimentalMacImeEnabled())
        {
            return;
        }

        if (_macOSNativeImeBridge is not null)
        {
            return;
        }

        Window? window = Window.Current;
        if (window is null)
        {
            LogMacIme("Window.Current is null. Cannot create native bridge.");
            return;
        }

        nint handle = TryGetNativeWindowHandle(window);
        if (handle == nint.Zero)
        {
            LogMacIme("Failed to resolve native macOS window handle.");
            return;
        }

        _macOSNativeImeBridge = MacOSNativeImeBridge.TryCreate(this, handle, DispatcherQueue);
        LogMacIme($"Native bridge creation complete. Available={_macOSNativeImeBridge is { IsAvailable: true }} handle=0x{handle:X}");
    }

    private static nint TryGetNativeWindowHandle(Window window)
    {
        object? nativeWindow = WindowHelper.GetNativeWindow(window);
        if (nativeWindow is null)
        {
            return nint.Zero;
        }

        PropertyInfo? handleProperty = nativeWindow.GetType().GetProperty(
            "Handle",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (handleProperty?.GetValue(nativeWindow) is nint handle)
        {
            LogMacIme($"Resolved native window handle from reflected Handle property: 0x{handle:X}");
            return handle;
        }

        if (handleProperty?.GetValue(nativeWindow) is IntPtr intPtrHandle)
        {
            LogMacIme($"Resolved native window IntPtr handle from reflected Handle property: 0x{intPtrHandle.ToInt64():X}");
            return intPtrHandle;
        }

        if (handleProperty?.GetValue(nativeWindow) is long longHandle)
        {
            LogMacIme($"Resolved native window long handle from reflected Handle property: 0x{longHandle:X}");
            return new nint(longHandle);
        }

        LogMacIme($"Reflected native window type {nativeWindow.GetType().FullName} did not expose a readable Handle property.");
        return nint.Zero;
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

    private void HandleMacOSNativeTextInput(string text)
    {
        LogMacIme($"Committed text from native bridge: '{text.Replace("\n", "\\n")}'");
        if (!string.IsNullOrEmpty(text))
        {
            InsertText(text);
        }
    }

    private void HandleMacOSNativeCommand(string command)
    {
        LogMacIme($"Native command received: {command}");
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
            return;
        }

        if (command == "paste:")
        {
            _ = PasteAsync();
        }
    }

    private sealed class MacOSNativeImeBridge : IDisposable
    {
        private readonly GCHandle _selfHandle;
        private readonly InsertTextDelegate _insertTextDelegate;
        private readonly CommandDelegate _commandDelegate;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly WeakReference<TextView> _owner;
        private nint _bridgeHandle;

        private MacOSNativeImeBridge(TextView owner, nint bridgeHandle, DispatcherQueue dispatcherQueue)
        {
            _owner = new WeakReference<TextView>(owner);
            _dispatcherQueue = dispatcherQueue;
            _bridgeHandle = bridgeHandle;
            _selfHandle = GCHandle.Alloc(this);
            _insertTextDelegate = OnInsertText;
            _commandDelegate = OnCommand;
        }

        public bool IsAvailable => _bridgeHandle != nint.Zero;

        public bool IsFocused => _bridgeHandle != nint.Zero && NativeMethods.unoedit_ime_is_focused(_bridgeHandle);

        public static MacOSNativeImeBridge? TryCreate(TextView owner, nint windowHandle, DispatcherQueue dispatcherQueue)
        {
            var session = new MacOSNativeImeBridge(owner, nint.Zero, dispatcherQueue);
            nint context = GCHandle.ToIntPtr(session._selfHandle);
            LogMacIme($"Creating native bridge with window handle 0x{windowHandle:X}.");
            nint bridgeHandle = NativeMethods.unoedit_ime_create(
                windowHandle,
                context,
                Marshal.GetFunctionPointerForDelegate(session._insertTextDelegate),
                Marshal.GetFunctionPointerForDelegate(session._commandDelegate));

            if (bridgeHandle == nint.Zero)
            {
                LogMacIme("Native bridge creation returned null handle.");
                session.Dispose();
                return null;
            }

            session._bridgeHandle = bridgeHandle;
            LogMacIme($"Native bridge created. Bridge handle=0x{bridgeHandle:X}");
            return session;
        }

        public void Focus()
        {
            if (_bridgeHandle != nint.Zero)
            {
                LogMacIme($"Requesting native bridge focus. Bridge handle=0x{_bridgeHandle:X}");
                NativeMethods.unoedit_ime_focus(_bridgeHandle, true);
            }
        }

        public void UpdateCaretRect(Rect rect)
        {
            if (_bridgeHandle == nint.Zero || rect == Rect.Empty)
            {
                return;
            }

            LogMacIme($"Updating native caret rect to x={rect.X:F1}, y={rect.Y:F1}, w={rect.Width:F1}, h={rect.Height:F1}");
            NativeMethods.unoedit_ime_update_caret_rect(_bridgeHandle, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void Dispose()
        {
            if (_bridgeHandle != nint.Zero)
            {
                LogMacIme($"Destroying native bridge. Bridge handle=0x{_bridgeHandle:X}");
                NativeMethods.unoedit_ime_destroy(_bridgeHandle);
                _bridgeHandle = nint.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }
        }

        private static void OnInsertText(nint context, nint utf8Text)
        {
            if (GCHandle.FromIntPtr(context).Target is not MacOSNativeImeBridge session)
            {
                return;
            }

            string? text = Marshal.PtrToStringUTF8(utf8Text);
            if (string.IsNullOrEmpty(text) || !session._owner.TryGetTarget(out TextView? owner))
            {
                LogMacIme("Native insert-text callback arrived without usable text or owner.");
                return;
            }

            session._dispatcherQueue.TryEnqueue(() => owner.HandleMacOSNativeTextInput(text));
        }

        private static void OnCommand(nint context, nint utf8Command)
        {
            if (GCHandle.FromIntPtr(context).Target is not MacOSNativeImeBridge session)
            {
                return;
            }

            string? command = Marshal.PtrToStringUTF8(utf8Command);
            if (string.IsNullOrEmpty(command) || !session._owner.TryGetTarget(out TextView? owner))
            {
                LogMacIme("Native command callback arrived without usable command or owner.");
                return;
            }

            session._dispatcherQueue.TryEnqueue(() => owner.HandleMacOSNativeCommand(command));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InsertTextDelegate(nint context, nint utf8Text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CommandDelegate(nint context, nint utf8Command);

        private static class NativeMethods
        {
            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern nint unoedit_ime_create(
                nint windowHandle,
                nint managedContext,
                nint insertTextCallback,
                nint commandCallback);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_destroy(nint bridgeHandle);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_focus(nint bridgeHandle, [MarshalAs(UnmanagedType.I1)] bool focus);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool unoedit_ime_is_focused(nint bridgeHandle);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_update_caret_rect(
                nint bridgeHandle,
                double x,
                double y,
                double width,
                double height);
        }
    }
#else
    partial void InitializePlatformInputBridge()
    {
    }

    partial void UpdatePlatformInputBridge()
    {
    }

    partial void FocusPlatformInputBridge()
    {
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        return false;
    }
#endif
}