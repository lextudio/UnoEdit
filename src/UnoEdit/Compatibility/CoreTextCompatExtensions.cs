using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Foundation;
#if !WINDOWS_APP_SDK
using Uno.UI.Xaml;
using LeXtudio.UI.Text.Core;
#endif

namespace Windows.UI.Text.Core
{
    /// <summary>
    /// Event args for CoreTextEditContext.CommandReceived, which is not available in the Windows App SDK version of CoreTextEditContext. This class is used to allow shared code to subscribe to command events without needing conditional compilation.
    /// </summary>
    public class CoreTextCommandReceivedEventArgs : EventArgs
    {
        public string Command { get; }
        public bool Handled { get; set; }

        public CoreTextCommandReceivedEventArgs(string command)
        {
            Command = command;
        }
    }
}

namespace UnoEdit.Skia.Desktop.Controls
{

    internal static class CoreTextCompatExtensions
    {
        extension(CoreTextEditContext context)
        {
            public void SyncState(
                Rect caretRect,
                Rect controlRect,
                double scale,
                int selectionStart,
                int selectionEnd,
                Window window)
            {
                context.NotifyLayoutChanged();
                context.NotifySelectionChanged(new CoreTextRange
                {
                    StartCaretPosition = Math.Min(selectionStart, selectionEnd),
                    EndCaretPosition = Math.Max(selectionStart, selectionEnd),
                });
                context.RasterizationScale = scale;
                context.NotifyCaretRectChanged(caretRect.X, caretRect.Y, caretRect.Width, caretRect.Height);
            }


#if WINDOWS_APP_SDK
        public bool AttachToCurrentWindow(Window? window)
        {
            return true;
        }

        public bool ProcessKeyEvent(
            int virtualKey,
            bool shiftPressed,
            bool controlPressed,
            char? unicodeKey = null)
        {
            return false;
        }

        public void Dispose()
        {
            // IMPORTANT: CoreTextEditContext in Windows App SDK does not implement IDisposable, so we cannot rely on using statements for cleanup. This method is a no-op and is only here to allow shared code to call Dispose without needing conditional compilation.
            _ = context;
        }

        public event EventHandler<CoreTextCommandReceivedEventArgs> CommandReceived
        {
            add { }
            remove { }
        }
#endif
        }

        extension(CoreTextLayoutRequestedEventArgs args)
        {
            public void ApplyLayoutBoundsCompat(Rect caretRect, Rect controlRect, Window? window)
            {
                Rect screenCaretRect = caretRect;
                Rect screenControlRect = controlRect;

#if WINDOWS_APP_SDK
            nint hwnd = window is null ? nint.Zero : (nint)WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (hwnd != nint.Zero && TryGetClientOriginInScreen(hwnd, out Point clientOrigin))
            {
                screenCaretRect = new Rect(caretRect.X + clientOrigin.X, caretRect.Y + clientOrigin.Y, caretRect.Width, caretRect.Height);
                screenControlRect = new Rect(controlRect.X + clientOrigin.X, controlRect.Y + clientOrigin.Y, controlRect.Width, controlRect.Height);
            }
#endif

#if WINDOWS_APP_SDK
            args.Request.LayoutBounds.TextBounds = screenCaretRect;
            args.Request.LayoutBounds.ControlBounds = screenControlRect;
#else
                args.Request.LayoutBounds.TextBounds = new CoreTextRect
                {
                    X = screenCaretRect.X,
                    Y = screenCaretRect.Y,
                    Width = screenCaretRect.Width,
                    Height = screenCaretRect.Height,
                };
                args.Request.LayoutBounds.ControlBounds = new CoreTextRect
                {
                    X = screenControlRect.X,
                    Y = screenControlRect.Y,
                    Width = screenControlRect.Width,
                    Height = screenControlRect.Height,
                };
#endif
            }
        }

#if WINDOWS_APP_SDK
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
#endif
    }
}