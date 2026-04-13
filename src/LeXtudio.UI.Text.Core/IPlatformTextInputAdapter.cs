using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Internal contract for platform-specific text input adapters.
    /// Each adapter translates native IME events into <see cref="CoreTextEditContext"/> events.
    /// Consumers never see or reference this interface directly.
    /// </summary>
    internal interface IPlatformTextInputAdapter : IDisposable
    {
        /// <summary>Attach the adapter to a native window and start listening for IME events.</summary>
        /// <param name="windowHandle">The native window handle (HWND on Windows, NSWindow pointer on macOS, X11 window on Linux).</param>
        /// <param name="displayHandle">The native display handle (X11 Display* on Linux, ignored on other platforms).</param>
        /// <param name="context">The <see cref="CoreTextEditContext"/> whose events this adapter will raise.</param>
        bool Attach(nint windowHandle, nint displayHandle, CoreTextEditContext context);

        /// <summary>Notify the adapter that the caret position or size has changed so it can reposition the IME candidate window.</summary>
        /// <param name="x">Caret X in window-relative coordinates.</param>
        /// <param name="y">Caret Y in window-relative coordinates.</param>
        /// <param name="width">Caret width.</param>
        /// <param name="height">Caret height.</param>
        /// <param name="scale">Display rasterization scale (DPI factor).</param>
        void NotifyCaretRectChanged(double x, double y, double width, double height, double scale);

        /// <summary>Notify the adapter that the context has gained keyboard focus.</summary>
        void NotifyFocusEnter();

        /// <summary>Notify the adapter that the context has lost keyboard focus.</summary>
        void NotifyFocusLeave();

        /// <summary>
        /// Forward a key event to the platform IME. Returns true if the IME consumed the key.
        /// On platforms without native IME key forwarding this returns false.
        /// </summary>
        /// <param name="virtualKey">The virtual key code (cast from <c>Windows.System.VirtualKey</c>).</param>
        /// <param name="shiftPressed">Whether the Shift modifier is active.</param>
        /// <param name="controlPressed">Whether the Control modifier is active.</param>
        /// <param name="unicodeKey">Optional Unicode character for keys that map to VirtualKey.None.</param>
        bool ProcessKeyEvent(int virtualKey, bool shiftPressed, bool controlPressed, char? unicodeKey = null) => false;
    }
}
