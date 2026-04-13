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
        /// <param name="context">The <see cref="CoreTextEditContext"/> whose events this adapter will raise.</param>
        bool Attach(nint windowHandle, CoreTextEditContext context);

        /// <summary>Notify the adapter that the caret position or size has changed so it can reposition the IME candidate window.</summary>
        void NotifyCaretRectChanged(double x, double y, double width, double height);

        /// <summary>Notify the adapter that the context has gained keyboard focus.</summary>
        void NotifyFocusEnter();

        /// <summary>Notify the adapter that the context has lost keyboard focus.</summary>
        void NotifyFocusLeave();
    }
}
