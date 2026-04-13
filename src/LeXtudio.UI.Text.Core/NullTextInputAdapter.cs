using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// No-op adapter used on platforms where native text input support is not yet implemented.
    /// All operations are safe to call but have no effect.
    /// </summary>
    internal sealed class NullTextInputAdapter : IPlatformTextInputAdapter
    {
        /// <inheritdoc />
        public bool Attach(nint windowHandle, CoreTextEditContext context) => false;

        /// <inheritdoc />
        public void NotifyCaretRectChanged(double x, double y, double width, double height) { }

        /// <inheritdoc />
        public void NotifyFocusEnter() { }

        /// <inheritdoc />
        public void NotifyFocusLeave() { }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
