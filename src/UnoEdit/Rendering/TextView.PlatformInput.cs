using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml;
using UnoEdit.Skia.Desktop.Controls.Platform.Linux;
using UnoEdit.Skia.Desktop.Controls.Platform.Windows;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{
    // -----------------------------------------------------------------------
    // Shared: runtime OS detection (net10.0-desktop is a single cross-platform binary)
    // -----------------------------------------------------------------------

    private static readonly bool s_isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly bool s_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    // -----------------------------------------------------------------------
    // macOS IME fields
    // -----------------------------------------------------------------------

    private const string ExperimentalMacImeEnvVar = "UNOEDIT_ENABLE_EXPERIMENTAL_MACOS_IME";
    private const string ExperimentalMacImeDebugEnvVar = "UNOEDIT_DEBUG_MACOS_IME";
    private MacOSNativeImeBridge? _macOSNativeImeBridge;

    // -----------------------------------------------------------------------
    // Windows / Linux IME fields
    // -----------------------------------------------------------------------

    private static readonly bool s_debugWin32Ime =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_WIN32_IME"), "1", StringComparison.Ordinal);

    private static readonly string s_win32ImeLogPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unoedit_win32_ime.log");

    // Composition state tracked in the document (Windows/Linux).
    private bool _isComposing;
    private int _compositionStartOffset;
    private int _compositionLength;

    // Platform-specific bridges (at most one is non-null at runtime).
    private Win32ImeBridge? _win32ImeBridge;
    private LinuxImeBridge? _linuxImeBridge;

    // -----------------------------------------------------------------------
    // Logging helpers
    // -----------------------------------------------------------------------

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

    private static void LogWin32Ime(string message)
    {
        if (!s_debugWin32Ime)
        {
            return;
        }

        try
        {
            System.IO.File.AppendAllText(s_win32ImeLogPath,
                $"{DateTime.Now:HH:mm:ss.fff} [TextView] {message}{Environment.NewLine}");
        }
        catch { }
    }

    // -----------------------------------------------------------------------
    // Partial method implementations — runtime OS dispatch
    // -----------------------------------------------------------------------

    partial void InitializePlatformInputBridge()
    {
        if (s_isMacOS)
        {
            LogMacIme($"InitializePlatformInputBridge called. ImeEnabled={IsExperimentalMacImeEnabled()}");
            if (!IsExperimentalMacImeEnabled())
            {
                LogMacIme($"Experimental bridge disabled. Set {ExperimentalMacImeEnvVar}=1 to enable.");
                return;
            }

            LogMacIme("Initializing experimental macOS IME bridge.");
            Loaded += OnPlatformInputLoaded;
            Unloaded += OnPlatformInputUnloaded;
        }
        else if (s_isWindows)
        {
            LogWin32Ime("InitializePlatformInputBridge: Windows detected, registering Loaded/Unloaded.");
            Loaded += OnPlatformInputLoaded;
            Unloaded += OnPlatformInputUnloaded;
        }
        else if (s_isLinux)
        {
            Loaded += OnPlatformInputLoaded;
            Unloaded += OnPlatformInputUnloaded;
        }
    }

    partial void UpdatePlatformInputBridge()
    {
        if (s_isMacOS)
        {
            if (!IsExperimentalMacImeEnabled())
            {
                return;
            }

            _macOSNativeImeBridge?.UpdateCaretRect(CalculatePlatformInputCaretRect());
        }
        else
        {
            Rect rect = CalculatePlatformInputCaretRect();
            if (_win32ImeBridge is { } win32)
            {
                LogWin32Ime($"UpdatePlatformInputBridge: caretRect={rect.X:F1},{rect.Y:F1} {rect.Width:F1}x{rect.Height:F1}");
                win32.CaretRect = rect;
                win32.PositionImeWindow();
            }
            else if (_linuxImeBridge is { IsAvailable: true } linux)
            {
                linux.UpdateCursorLocation(rect);
            }
        }
    }

    partial void FocusPlatformInputBridge()
    {
        if (s_isMacOS)
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
        else
        {
            _linuxImeBridge?.FocusIn();
        }
    }

    private partial bool ShouldDeferToPlatformTextInput(bool controlPressed)
    {
        if (s_isMacOS)
        {
            bool shouldDefer = IsExperimentalMacImeEnabled() && !controlPressed && _macOSNativeImeBridge?.IsFocused == true;
            if (shouldDefer)
            {
                LogMacIme("Managed key handling deferred to native AppKit responder.");
            }

            return shouldDefer;
        }

        // Windows and Linux do NOT defer Uno key events; IME integration is handled
        // via WndProc subclassing (Win32) and explicit key forwarding (Linux IBus).
        return false;
    }

    /// <summary>
    /// Forwards a key to the Linux IBus bridge before normal UnoEdit processing.
    /// Returns true if IBus consumed the key (caller should suppress normal handling).
    /// Only active on Linux; always returns false on other platforms.
    /// </summary>
    internal bool TryHandleWithLinuxIme(Windows.System.VirtualKey key, bool controlPressed, bool shiftPressed, char? unicodeKey = null)
    {
        if (_linuxImeBridge is not { IsAvailable: true } bridge)
        {
            return false;
        }

        // Control-key shortcuts are handled by the editor, not the IME.
        if (controlPressed)
        {
            return false;
        }

        uint keyval = ConvertToX11Keysym(key, shiftPressed);

        // For OEM keys (VirtualKey.None on Skia/Linux), the keysym equals the
        // Unicode codepoint for printable ASCII characters.
        if (keyval == 0 && unicodeKey.HasValue && unicodeKey.Value >= 0x20 && unicodeKey.Value < 0x7F)
        {
            keyval = (uint)unicodeKey.Value;
        }

        if (keyval == 0)
        {
            return false; // unknown key — let UnoEdit handle it
        }

        uint state = GetX11ModifierState(shiftPressed, controlPressed);
        return bridge.ProcessKeyEvent(keyval, 0, state);
    }

    // -----------------------------------------------------------------------
    // Loaded / Unloaded — shared entry point, runtime dispatch
    // -----------------------------------------------------------------------

    private void OnPlatformInputLoaded(object sender, RoutedEventArgs e)
    {
        if (s_isMacOS)
        {
            LogMacIme("TextView loaded.");
            EnsureMacOSNativeImeBridge();
        }
        else if (s_isWindows)
        {
            LogWin32Ime("OnPlatformInputLoaded: Windows.");
            EnsureWin32ImeBridge();
        }
        else if (s_isLinux)
        {
            EnsureLinuxImeBridge();
        }

        UpdatePlatformInputBridge();
    }

    private void OnPlatformInputUnloaded(object sender, RoutedEventArgs e)
    {
        if (s_isMacOS)
        {
            LogMacIme("TextView unloaded. Disposing native bridge.");
            _macOSNativeImeBridge?.Dispose();
            _macOSNativeImeBridge = null;
        }
        else
        {
            LogWin32Ime("OnPlatformInputUnloaded: disposing bridges.");
            _win32ImeBridge?.Dispose();
            _win32ImeBridge = null;
            _linuxImeBridge?.Dispose();
            _linuxImeBridge = null;
        }
    }

    // -----------------------------------------------------------------------
    // macOS bridge lifecycle
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    // Windows / Linux bridge lifecycle
    // -----------------------------------------------------------------------

    private void EnsureWin32ImeBridge()
    {
        if (_win32ImeBridge is not null)
        {
            LogWin32Ime("EnsureWin32ImeBridge: bridge already exists, skipping.");
            return;
        }

        nint hwnd = TryGetNativeWindowHandle(Window.Current);
        LogWin32Ime($"EnsureWin32ImeBridge: hwnd=0x{hwnd:X}");
        if (hwnd == nint.Zero)
        {
            LogWin32Ime("EnsureWin32ImeBridge: hwnd is zero, cannot create bridge.");
            return;
        }

        _win32ImeBridge = Win32ImeBridge.TryCreate(hwnd);
        LogWin32Ime($"EnsureWin32ImeBridge: bridge created={_win32ImeBridge is not null}");
        if (_win32ImeBridge is null)
        {
            return;
        }

        _win32ImeBridge.CompositionStarted += () => HandleCompositionStart();
        _win32ImeBridge.CompositionUpdated += text => HandleCompositionUpdate(text);
        _win32ImeBridge.CompositionEnded += () => HandleCompositionEnd();
        _win32ImeBridge.TextCommitted += text => HandleCompositionCommit(text);
        LogWin32Ime("EnsureWin32ImeBridge: callbacks wired.");
    }

    private void EnsureLinuxImeBridge()
    {
        if (_linuxImeBridge is not null)
        {
            return;
        }

        _linuxImeBridge = LinuxImeBridge.TryCreate();
        if (_linuxImeBridge is null)
        {
            return;
        }

        _linuxImeBridge.TextCommitted += text => HandleCompositionCommit(text);
        _linuxImeBridge.PreeditUpdated += text => HandleCompositionUpdate(text);
        _linuxImeBridge.PreeditEnded += () => HandleCompositionEnd();

        _linuxImeBridge.FocusIn();
    }

    // -----------------------------------------------------------------------
    // Composition document management (Windows / Linux)
    // -----------------------------------------------------------------------

    internal void HandleCompositionStart()
    {
        LogWin32Ime($"HandleCompositionStart: offset={CurrentOffset}");
        _compositionStartOffset = CurrentOffset;
        _compositionLength = 0;
        _isComposing = true;
        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionUpdate(string text)
    {
        LogWin32Ime($"HandleCompositionUpdate: text='{text}' isComposing={_isComposing} startOffset={_compositionStartOffset} prevLen={_compositionLength}");
        if (_document is null)
        {
            LogWin32Ime("HandleCompositionUpdate: document is null, aborting.");
            return;
        }

        if (!_isComposing)
        {
            HandleCompositionStart();
        }

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
        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionEnd()
    {
        LogWin32Ime($"HandleCompositionEnd: isComposing={_isComposing} compositionLength={_compositionLength}");
        if (_document is null || !_isComposing)
        {
            _isComposing = false;
            _compositionLength = 0;
            return;
        }

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
        UpdatePlatformInputBridge();
    }

    internal void HandleCompositionCommit(string text)
    {
        LogWin32Ime($"HandleCompositionCommit: text='{text}' isComposing={_isComposing} startOffset={_compositionStartOffset} compositionLength={_compositionLength}");
        if (_document is null)
        {
            LogWin32Ime("HandleCompositionCommit: document is null, aborting.");
            return;
        }

        int insertAt = _isComposing ? _compositionStartOffset : CurrentOffset;
        int removeLen = _isComposing ? _compositionLength : 0;

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

        LogMacIme($"CalculatePlatformInputCaretRect pre-transform x={x:F1}, y={y:F1}, visualRow={visualRow}, logicalColumn={logicalColumn}, GutterWidth={GutterWidth:F1}, TextLeftPadding={TextLeftPadding:F1}, HOffset={TextScrollViewer.HorizontalOffset:F1}, VOffset={TextScrollViewer.VerticalOffset:F1}");

        GeneralTransform transform = RootBorder.TransformToVisual(null);
        Point point = transform.TransformPoint(new Point(x, y));

        LogMacIme($"CalculatePlatformInputCaretRect post-transform point.X={point.X:F1}, point.Y={point.Y:F1} returnedRect X={point.X:F1} Y={point.Y + 3d:F1} W=2.0 H=16.0");
        return new Rect(point.X, point.Y + 3d, 2d, 16d);
    }

    private static nint TryGetNativeWindowHandle(Window? window)
    {
        if (window is null)
        {
            return nint.Zero;
        }

        object? nativeWindow = WindowHelper.GetNativeWindow(window);
        if (nativeWindow is null)
        {
            LogWin32Ime("TryGetNativeWindowHandle: GetNativeWindow returned null.");
            LogMacIme("TryGetNativeWindowHandle: GetNativeWindow returned null.");
            return nint.Zero;
        }

        string nativeTypeName = nativeWindow.GetType().FullName ?? string.Empty;
        LogWin32Ime($"TryGetNativeWindowHandle: native window type={nativeTypeName}");
        LogMacIme($"TryGetNativeWindowHandle: native window type={nativeTypeName}");

        // Windows Skia path: GetNativeWindow returns System.Windows.Window (WPF shell).
        // System.Windows.Window has no Handle property; use WindowInteropHelper instead.
        if (nativeTypeName == "System.Windows.Window")
        {
            try
            {
                Type? helperType = Type.GetType(
                    "System.Windows.Interop.WindowInteropHelper, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                helperType ??= Type.GetType("System.Windows.Interop.WindowInteropHelper, PresentationFramework");
                if (helperType is null)
                {
                    LogWin32Ime("TryGetNativeWindowHandle: could not load WindowInteropHelper type.");
                    return nint.Zero;
                }

                object? helper = Activator.CreateInstance(helperType, nativeWindow);
                PropertyInfo? handleProp = helperType.GetProperty("Handle",
                    BindingFlags.Instance | BindingFlags.Public);

                if (handleProp?.GetValue(helper) is IntPtr hwndIntPtr)
                {
                    LogWin32Ime($"TryGetNativeWindowHandle: HWND via WindowInteropHelper=0x{hwndIntPtr.ToInt64():X}");
                    return hwndIntPtr;
                }
            }
            catch (Exception ex)
            {
                LogWin32Ime($"TryGetNativeWindowHandle: WindowInteropHelper reflection failed: {ex.Message}");
            }

            return nint.Zero;
        }

        // Windows Skia path (newer Uno): Uno.UI.NativeElementHosting.Win32NativeWindow
        // Reflect to find the HWND from whatever property/field exposes it.
        if (nativeTypeName.Contains("Win32NativeWindow", StringComparison.Ordinal))
        {
            // Dump all members in debug mode so we can identify the right property once.
            if (s_debugWin32Ime)
            {
                foreach (PropertyInfo p in nativeWindow.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    LogWin32Ime($"  Win32NativeWindow property: {p.Name} ({p.PropertyType.Name})");
                }

                foreach (FieldInfo f in nativeWindow.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    LogWin32Ime($"  Win32NativeWindow field: {f.Name} ({f.FieldType.Name})");
                }
            }

            // Try common names for the HWND property/field.
            foreach (string name in new[] { "Hwnd", "HWnd", "Handle", "WindowHandle", "NativeHandle", "Pointer", "hwnd", "_hwnd" })
            {
                PropertyInfo? p = nativeWindow.GetType().GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p is not null)
                {
                    object? v = p.GetValue(nativeWindow);
                    if (v is nint np && np != nint.Zero) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via property '{name}'=0x{np:X}"); return np; }
                    if (v is IntPtr ip && ip != IntPtr.Zero) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via property '{name}'=0x{ip.ToInt64():X}"); return ip; }
                    if (v is long lv && lv != 0) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via property '{name}'=0x{lv:X}"); return new nint(lv); }
                    LogWin32Ime($"TryGetNativeWindowHandle: property '{name}' found but value={v}");
                }

                FieldInfo? f = nativeWindow.GetType().GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f is not null)
                {
                    object? v = f.GetValue(nativeWindow);
                    if (v is nint np && np != nint.Zero) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via field '{name}'=0x{np:X}"); return np; }
                    if (v is IntPtr ip && ip != IntPtr.Zero) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via field '{name}'=0x{ip.ToInt64():X}"); return ip; }
                    if (v is long lv && lv != 0) { LogWin32Ime($"TryGetNativeWindowHandle: found HWND via field '{name}'=0x{lv:X}"); return new nint(lv); }
                    LogWin32Ime($"TryGetNativeWindowHandle: field '{name}' found but value={v}");
                }
            }

            LogWin32Ime("TryGetNativeWindowHandle: no known HWND member found on Win32NativeWindow.");
            return nint.Zero;
        }

        // macOS / other platforms: reflect on the Handle property directly.
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
            LogMacIme($"Resolved native window IntPtr handle: 0x{intPtrHandle.ToInt64():X}");
            return intPtrHandle;
        }

        if (handleProperty?.GetValue(nativeWindow) is long longHandle)
        {
            LogMacIme($"Resolved native window long handle: 0x{longHandle:X}");
            return new nint(longHandle);
        }

        LogMacIme($"Native window type {nativeTypeName} did not expose a readable Handle property.");
        return nint.Zero;
    }

    // -----------------------------------------------------------------------
    // X11 keysym / modifier helpers (Linux IBus)
    // -----------------------------------------------------------------------

    private static uint ConvertToX11Keysym(Windows.System.VirtualKey key, bool shiftPressed)
    {
        if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
        {
            int offset = (int)key - (int)Windows.System.VirtualKey.A;
            return shiftPressed ? (uint)(0x41 + offset) : (uint)(0x61 + offset);
        }

        if (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9)
        {
            int digit = (int)key - (int)Windows.System.VirtualKey.Number0;
            if (shiftPressed)
            {
                // US keyboard shifted digits
                uint[] shifted = [0x29, 0x21, 0x40, 0x23, 0x24, 0x25, 0x5E, 0x26, 0x2A, 0x28]; // ) ! @ # $ % ^ & * (
                return digit < shifted.Length ? shifted[digit] : 0;
            }

            return (uint)(0x30 + digit);
        }

        return key switch
        {
            Windows.System.VirtualKey.Back => 0xFF08u,    // BackSpace
            Windows.System.VirtualKey.Tab => 0xFF09u,     // Tab
            Windows.System.VirtualKey.Enter => 0xFF0Du,   // Return
            Windows.System.VirtualKey.Escape => 0xFF1Bu,  // Escape
            Windows.System.VirtualKey.Space => 0x0020u,   // space
            Windows.System.VirtualKey.PageUp => 0xFF55u,
            Windows.System.VirtualKey.PageDown => 0xFF56u,
            Windows.System.VirtualKey.End => 0xFF57u,
            Windows.System.VirtualKey.Home => 0xFF50u,
            Windows.System.VirtualKey.Left => 0xFF51u,
            Windows.System.VirtualKey.Up => 0xFF52u,
            Windows.System.VirtualKey.Right => 0xFF53u,
            Windows.System.VirtualKey.Down => 0xFF54u,
            Windows.System.VirtualKey.Delete => 0xFFFFu,
            (Windows.System.VirtualKey)186 => shiftPressed ? 0x3Au : 0x3Bu, // : or ;
            (Windows.System.VirtualKey)187 => shiftPressed ? 0x2Bu : 0x3Du, // + or =
            (Windows.System.VirtualKey)188 => shiftPressed ? 0x3Cu : 0x2Cu, // < or ,
            (Windows.System.VirtualKey)189 => shiftPressed ? 0x5Fu : 0x2Du, // _ or -
            (Windows.System.VirtualKey)190 => shiftPressed ? 0x3Eu : 0x2Eu, // > or .
            (Windows.System.VirtualKey)191 => shiftPressed ? 0x3Fu : 0x2Fu, // ? or /
            (Windows.System.VirtualKey)219 => shiftPressed ? 0x7Bu : 0x5Bu, // { or [
            (Windows.System.VirtualKey)220 => shiftPressed ? 0x7Cu : 0x5Cu, // | or \
            (Windows.System.VirtualKey)221 => shiftPressed ? 0x7Du : 0x5Du, // } or ]
            (Windows.System.VirtualKey)222 => shiftPressed ? 0x22u : 0x27u, // " or '
            _ => 0u
        };
    }

    private static uint GetX11ModifierState(bool shiftPressed, bool controlPressed)
    {
        uint state = 0;
        if (shiftPressed)
        {
            state |= 0x0001; // ShiftMask
        }

        if (controlPressed)
        {
            state |= 0x0004; // ControlMask
        }

        var flags = Windows.UI.Core.CoreVirtualKeyStates.Down;
        if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(flags))
        {
            state |= 0x0008; // Mod1Mask (Alt)
        }

        return state;
    }

    // -----------------------------------------------------------------------
    // macOS native IME bridge (AppKit interop via libUnoEditMacInput.dylib)
    // -----------------------------------------------------------------------

    private sealed class MacOSNativeImeBridge : IDisposable
    {
        private static long s_nextEventId;
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
            try
            {
                double scale = NativeMethods.unoedit_ime_get_backing_scale(session._bridgeHandle);
                LogMacIme($"Native window backing scale={scale:F2}");
            }
            catch (Exception ex)
            {
                LogMacIme($"Failed to query native backing scale: {ex.Message}");
            }
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
                // assign an incrementing event id so native logs can be correlated deterministically
                long eventId = Interlocked.Increment(ref s_nextEventId);
                LogMacIme($"UpdateCaretRect pre-adjust id={eventId} -> x={rect.X:F3}, y={rect.Y:F3}, w={rect.Width:F3}, h={rect.Height:F3}");

                // Query native backing scale when available and align rectangle to device pixels
                double backingScale = 1.0;
                try
                {
                    backingScale = NativeMethods.unoedit_ime_get_backing_scale(_bridgeHandle);
                }
                catch
                {
                    // ignore - fallback to 1.0
                }
                LogMacIme($"UpdateCaretRect backingScale={backingScale:F2} id={eventId}");

                // Convert to device pixels, round to integer pixel boundaries, convert back to DIP
                double px = rect.X * backingScale;
                double py = rect.Y * backingScale;
                double pw = Math.Max(1.0, rect.Width * backingScale);
                double ph = Math.Max(1.0, rect.Height * backingScale);

                double rpx = Math.Round(px);
                double rpy = Math.Round(py);
                double rpw = Math.Max(1.0, Math.Round(pw));
                double rph = Math.Max(1.0, Math.Round(ph));

                var adjusted = new Rect(rpx / backingScale, rpy / backingScale, rpw / backingScale, rph / backingScale);
                LogMacIme($"UpdateCaretRect adjusted -> x={adjusted.X:F3}, y={adjusted.Y:F3}, w={adjusted.Width:F3}, h={adjusted.Height:F3} (pixels: {rpx},{rpy},{rpw},{rph}) id={eventId}");

                NativeMethods.unoedit_ime_update_caret_rect(_bridgeHandle, (ulong)eventId, adjusted.X, adjusted.Y, adjusted.Width, adjusted.Height);
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
                ulong eventId,
                double x,
                double y,
                double width,
                double height);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern double unoedit_ime_get_backing_scale(nint bridgeHandle);
        }
    }
}
