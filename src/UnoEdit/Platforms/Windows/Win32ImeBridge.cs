using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls.Platform.Windows;

/// <summary>
/// Bridges Win32 IME messages (WM_IME_*) to UnoEdit text composition events by
/// subclassing the Uno-owned HWND's WndProc. No external DLL required.
/// </summary>
internal sealed class Win32ImeBridge : IDisposable
{
    private const int WM_IME_STARTCOMPOSITION = 0x010D;
    private const int WM_IME_ENDCOMPOSITION = 0x010E;
    private const int WM_IME_COMPOSITION = 0x010F;
    private const int GWLP_WNDPROC = -4;

    private static readonly bool s_debugEnabled =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_WIN32_IME"), "1", StringComparison.Ordinal);

    private readonly nint _hwnd;
    private nint _originalWndProc;
    private WndProcDelegate? _wndProcDelegate; // held alive to prevent GC

    private bool _disposed;

    // Invoked on UI thread (WndProc runs on the window-creator thread).
    public event Action? CompositionStarted;
    public event Action<string>? CompositionUpdated;
    public event Action? CompositionEnded;
    public event Action<string>? TextCommitted;

    // Updated by owner before IME positioning calls.
    public Rect CaretRect { get; set; }

    private delegate nint WndProcDelegate(nint hwnd, int msg, nint wParam, nint lParam);

    private Win32ImeBridge(nint hwnd)
    {
        _hwnd = hwnd;
    }

    public static Win32ImeBridge? TryCreate(nint hwnd)
    {
        if (hwnd == nint.Zero)
        {
            return null;
        }

        var bridge = new Win32ImeBridge(hwnd);
        try
        {
            bridge._wndProcDelegate = bridge.WndProc;
            nint newProc = Marshal.GetFunctionPointerForDelegate(bridge._wndProcDelegate);
            bridge._originalWndProc = NativeMethods.SetWindowLongPtrW(hwnd, GWLP_WNDPROC, newProc);

            if (bridge._originalWndProc == nint.Zero)
            {
                Log("SetWindowLongPtrW returned zero; cannot subclass window.");
                bridge.Dispose();
                return null;
            }

            Log($"WndProc subclassed. hwnd=0x{hwnd:X}");
            return bridge;
        }
        catch (Exception ex)
        {
            Log($"TryCreate failed: {ex.Message}");
            bridge.Dispose();
            return null;
        }
    }

    public void PositionImeWindow()
    {
        Rect rect = CaretRect;
        if (rect == Rect.Empty)
        {
            return;
        }

        nint himc = NativeMethods.ImmGetContext(_hwnd);
        if (himc == nint.Zero)
        {
            return;
        }

        try
        {
            double dpi = NativeMethods.GetDpiForWindow(_hwnd) / 96.0;

            // Convert DIP screen coordinates to physical client-area pixels.
            var screenPt = new NativeMethods.POINT
            {
                x = (int)(rect.X * dpi),
                y = (int)(rect.Y * dpi),
            };
            NativeMethods.ScreenToClient(_hwnd, ref screenPt);

            var compForm = new NativeMethods.COMPOSITIONFORM
            {
                dwStyle = NativeMethods.CFS_POINT | NativeMethods.CFS_FORCE_POSITION,
                ptCurrentPos = screenPt,
            };
            NativeMethods.ImmSetCompositionWindow(himc, ref compForm);

            var candForm = new NativeMethods.CANDIDATEFORM
            {
                dwIndex = 0,
                dwStyle = NativeMethods.CFS_CANDIDATEPOS,
                ptCurrentPos = screenPt,
            };
            NativeMethods.ImmSetCandidateWindow(himc, ref candForm);

            Log($"PositionImeWindow x={screenPt.x} y={screenPt.y}");
        }
        finally
        {
            NativeMethods.ImmReleaseContext(_hwnd, himc);
        }
    }

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
                NativeMethods.SetWindowLongPtrW(_hwnd, GWLP_WNDPROC, _originalWndProc);
                Log("WndProc restored.");
            }
            catch (Exception ex)
            {
                Log($"Restore WndProc failed: {ex.Message}");
            }
        }

        _wndProcDelegate = null;
    }

    // -----------------------------------------------------------------------
    // WndProc subclass
    // -----------------------------------------------------------------------

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_IME_STARTCOMPOSITION:
                Log("WM_IME_STARTCOMPOSITION");
                CompositionStarted?.Invoke();
                return 0; // prevent default IME window

            case WM_IME_COMPOSITION:
                HandleImeComposition(lParam);
                return 0;

            case WM_IME_ENDCOMPOSITION:
                Log("WM_IME_ENDCOMPOSITION");
                CompositionEnded?.Invoke();
                break; // allow DefWindowProc to clean up IME state
        }

        return NativeMethods.CallWindowProcW(_originalWndProc, hwnd, msg, wParam, lParam);
    }

    private void HandleImeComposition(nint lParam)
    {
        const int GCS_COMPSTR = 0x0008;
        const int GCS_RESULTSTR = 0x0800;

        int flags = (int)lParam.ToInt64();
        Log($"WM_IME_COMPOSITION flags=0x{flags:X}");

        if ((flags & GCS_COMPSTR) != 0)
        {
            string comp = GetCompositionString(GCS_COMPSTR);
            Log($"  GCS_COMPSTR='{comp}'");
            PositionImeWindow();
            CompositionUpdated?.Invoke(comp);
        }

        if ((flags & GCS_RESULTSTR) != 0)
        {
            string result = GetCompositionString(GCS_RESULTSTR);
            Log($"  GCS_RESULTSTR='{result}'");
            if (!string.IsNullOrEmpty(result))
            {
                TextCommitted?.Invoke(result);
            }
        }
    }

    private string GetCompositionString(int dwIndex)
    {
        nint himc = NativeMethods.ImmGetContext(_hwnd);
        if (himc == nint.Zero)
        {
            return string.Empty;
        }

        try
        {
            int byteCount = NativeMethods.ImmGetCompositionStringW(himc, dwIndex, nint.Zero, 0);
            if (byteCount <= 0)
            {
                return string.Empty;
            }

            byteCount = Math.Min(byteCount, 1024 * 128);
            byte[] buffer = new byte[byteCount];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                NativeMethods.ImmGetCompositionStringW(himc, dwIndex, handle.AddrOfPinnedObject(), byteCount);
            }
            finally
            {
                handle.Free();
            }

            return Encoding.Unicode.GetString(buffer, 0, byteCount);
        }
        finally
        {
            NativeMethods.ImmReleaseContext(_hwnd, himc);
        }
    }

    private static void Log(string message)
    {
        if (s_debugEnabled)
        {
            Console.Error.WriteLine($"[UnoEdit Win32 IME] {message}");
        }
    }

    // -----------------------------------------------------------------------
    // P/Invoke
    // -----------------------------------------------------------------------

    private static class NativeMethods
    {
        public const int CFS_POINT = 0x0002;
        public const int CFS_FORCE_POSITION = 0x0020;
        public const int CFS_CANDIDATEPOS = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        public struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CANDIDATEFORM
        {
            public int dwIndex;
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static extern nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        public static extern nint CallWindowProcW(nint lpPrevWndFunc, nint hWnd, int Msg, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ScreenToClient(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(nint hwnd);

        [DllImport("imm32.dll")]
        public static extern nint ImmGetContext(nint hWnd);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImmReleaseContext(nint hWnd, nint hIMC);

        [DllImport("imm32.dll")]
        public static extern int ImmGetCompositionStringW(nint hIMC, int dwIndex, nint lpBuf, int dwBufLen);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImmSetCompositionWindow(nint hIMC, ref COMPOSITIONFORM lpCompForm);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImmSetCandidateWindow(nint hIMC, ref CANDIDATEFORM lpCandidate);
    }
}
