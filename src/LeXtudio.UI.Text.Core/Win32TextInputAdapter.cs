using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Win32 IME adapter that subclasses the target HWND's WndProc to intercept
    /// <c>WM_IME_STARTCOMPOSITION</c>, <c>WM_IME_COMPOSITION</c>, and
    /// <c>WM_IME_ENDCOMPOSITION</c> messages and translates them into
    /// <see cref="CoreTextEditContext"/> events.
    /// </summary>
    internal sealed class Win32TextInputAdapter : IPlatformTextInputAdapter
    {
        // Win32 IME message constants
        private const int WM_IME_STARTCOMPOSITION = 0x010D;
        private const int WM_IME_ENDCOMPOSITION = 0x010E;
        private const int WM_IME_COMPOSITION = 0x010F;
        private const int GWLP_WNDPROC = -4;

        // IMM32 composition string indices
        private const int GCS_COMPSTR = 0x0008;
        private const int GCS_RESULTSTR = 0x0800;

        // IMM32 composition form styles
        private const int CFS_POINT = 0x0002;
        private const int CFS_FORCE_POSITION = 0x0020;
        private const int CFS_CANDIDATEPOS = 0x0040;

        private static readonly bool s_debugEnabled =
            string.Equals(
                Environment.GetEnvironmentVariable("LEXTUDIO_DEBUG_WIN32_IME"),
                "1",
                StringComparison.Ordinal);

        private nint _hwnd;
        private nint _originalWndProc;
        private WndProcDelegate? _wndProcDelegate; // prevent GC
        private CoreTextEditContext? _context;
        private bool _disposed;

        // Current caret rectangle in device-independent pixels (window-relative).
        private double _caretX;
        private double _caretY;
        private double _caretWidth;
        private double _caretHeight;

        private delegate nint WndProcDelegate(nint hwnd, int msg, nint wParam, nint lParam);

        /// <inheritdoc />
        public bool Attach(nint windowHandle, nint displayHandle, CoreTextEditContext context)
        {
            if (windowHandle == nint.Zero)
            {
                Log("Attach: window handle is zero.");
                return false;
            }

            _hwnd = windowHandle;
            _context = context;

            try
            {
                _wndProcDelegate = WndProc;
                nint newProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
                _originalWndProc = SetWindowLongPtrW(_hwnd, GWLP_WNDPROC, newProc);

                if (_originalWndProc == nint.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    Log($"Attach: SetWindowLongPtrW failed (err={err}).");
                    return false;
                }

                Log($"Attach: subclassed HWND 0x{_hwnd:X}.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Attach failed: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public void NotifyCaretRectChanged(double x, double y, double width, double height, double scale)
        {
            _caretX = x;
            _caretY = y;
            _caretWidth = width;
            _caretHeight = height;
            PositionImeWindow();
        }

        /// <inheritdoc />
        public void NotifyFocusEnter() { }

        /// <inheritdoc />
        public void NotifyFocusLeave() { }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_originalWndProc != nint.Zero && _hwnd != nint.Zero)
            {
                try
                {
                    SetWindowLongPtrW(_hwnd, GWLP_WNDPROC, _originalWndProc);
                    Log("Dispose: WndProc restored.");
                }
                catch (Exception ex)
                {
                    Log($"Dispose: restore failed: {ex.Message}");
                }
            }

            _wndProcDelegate = null;
            _context = null;
        }

        // ----- WndProc hook -----

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam)
        {
            switch (msg)
            {
                case WM_IME_STARTCOMPOSITION:
                    Log("WM_IME_STARTCOMPOSITION");
                    _context?.RaiseCompositionStarted();
                    return nint.Zero;

                case WM_IME_COMPOSITION:
                    HandleImeComposition(lParam);
                    return nint.Zero;

                case WM_IME_ENDCOMPOSITION:
                    Log("WM_IME_ENDCOMPOSITION");
                    _context?.RaiseCompositionCompleted();
                    break;
            }

            return CallWindowProcW(_originalWndProc, hwnd, msg, wParam, lParam);
        }

        private void HandleImeComposition(nint lParam)
        {
            int flags = (int)lParam.ToInt64();

            if ((flags & GCS_COMPSTR) != 0)
            {
                string comp = GetCompositionString(GCS_COMPSTR);
                Log($"GCS_COMPSTR: '{comp}'");
                PositionImeWindow();
                _context?.RaiseTextUpdating(new CoreTextTextUpdatingEventArgs(comp));
            }

            if ((flags & GCS_RESULTSTR) != 0)
            {
                string result = GetCompositionString(GCS_RESULTSTR);
                Log($"GCS_RESULTSTR: '{result}'");
                if (!string.IsNullOrEmpty(result))
                {
                    var request = new CoreTextTextRequest(result);
                    _context?.RaiseTextRequested(new CoreTextTextRequestedEventArgs(request));
                }
            }
        }

        private string GetCompositionString(int dwIndex)
        {
            nint himc = ImmGetContext(_hwnd);
            if (himc == nint.Zero)
            {
                return string.Empty;
            }

            try
            {
                int byteCount = ImmGetCompositionStringW(himc, dwIndex, nint.Zero, 0);
                if (byteCount <= 0)
                {
                    return string.Empty;
                }

                byteCount = Math.Min(byteCount, 128 * 1024);
                byte[] buffer = new byte[byteCount];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    ImmGetCompositionStringW(himc, dwIndex, handle.AddrOfPinnedObject(), byteCount);
                }
                finally
                {
                    handle.Free();
                }

                return Encoding.Unicode.GetString(buffer, 0, byteCount);
            }
            finally
            {
                ImmReleaseContext(_hwnd, himc);
            }
        }

        // ----- IME candidate window positioning -----

        private void PositionImeWindow()
        {
            if (_hwnd == nint.Zero)
            {
                return;
            }

            nint himc = ImmGetContext(_hwnd);
            if (himc == nint.Zero)
            {
                return;
            }

            try
            {
                double dpi = GetDpiForWindow(_hwnd) / 96.0;
                var screenPt = new POINT
                {
                    x = (int)(_caretX * dpi),
                    y = (int)(_caretY * dpi),
                };

                var compForm = new COMPOSITIONFORM
                {
                    dwStyle = CFS_POINT | CFS_FORCE_POSITION,
                    ptCurrentPos = screenPt,
                };
                ImmSetCompositionWindow(himc, ref compForm);

                var candForm = new CANDIDATEFORM
                {
                    dwIndex = 0,
                    dwStyle = CFS_CANDIDATEPOS,
                    ptCurrentPos = screenPt,
                };
                ImmSetCandidateWindow(himc, ref candForm);

                Log($"PositionImeWindow: x={screenPt.x} y={screenPt.y}");
            }
            finally
            {
                ImmReleaseContext(_hwnd, himc);
            }
        }

        // ----- Logging -----

        private static void Log(string message)
        {
            if (!s_debugEnabled)
            {
                return;
            }

            try
            {
                string path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "lextudio_win32_ime.log");
                System.IO.File.AppendAllText(
                    path,
                    $"{DateTime.Now:HH:mm:ss.fff} [Win32Adapter] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        // ----- Native structs / P/Invoke -----

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CANDIDATEFORM
        {
            public int dwIndex;
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern nint CallWindowProcW(nint lpPrevWndFunc, nint hWnd, int msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(nint hwnd);

        [DllImport("imm32.dll")]
        private static extern nint ImmGetContext(nint hWnd);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmReleaseContext(nint hWnd, nint hIMC);

        [DllImport("imm32.dll")]
        private static extern int ImmGetCompositionStringW(nint hIMC, int dwIndex, nint lpBuf, int dwBufLen);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmSetCompositionWindow(nint hIMC, ref COMPOSITIONFORM lpCompForm);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmSetCandidateWindow(nint hIMC, ref CANDIDATEFORM lpCandidate);
    }
}
