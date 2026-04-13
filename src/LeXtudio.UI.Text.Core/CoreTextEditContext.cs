using System;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Central event hub that raises text, selection, layout and composition events
    /// for adapters and editor hosts.
    /// Consumers obtain instances via <see cref="CoreTextServicesManager.CreateEditContext"/>.
    /// The underlying platform adapter is transparent — callers never see it.
    /// </summary>
    public sealed class CoreTextEditContext : IDisposable
    {
        private readonly IPlatformTextInputAdapter _adapter;

        /// <summary>Initializes a context with no platform adapter (useful for testing).</summary>
        public CoreTextEditContext()
        {
            _adapter = new NullTextInputAdapter();
        }

        /// <summary>Initializes a context backed by the specified platform adapter.</summary>
        internal CoreTextEditContext(IPlatformTextInputAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        // ----- Events (public surface for consumers) -----

        /// <summary>Occurs when the platform requests the current text.</summary>
        public event EventHandler<CoreTextTextRequestedEventArgs>? TextRequested;

        /// <summary>Occurs when text is being updated by the platform.</summary>
        public event EventHandler<CoreTextTextUpdatingEventArgs>? TextUpdating;

        /// <summary>Occurs when the platform requests the current selection.</summary>
        public event EventHandler<CoreTextSelectionRequestedEventArgs>? SelectionRequested;

        /// <summary>Occurs when the selection is being updated by the platform.</summary>
        public event EventHandler<CoreTextSelectionUpdatingEventArgs>? SelectionUpdating;

        /// <summary>Occurs when a layout measurement is requested by the platform.</summary>
        public event EventHandler<CoreTextLayoutRequestedEventArgs>? LayoutRequested;

        /// <summary>Occurs when IME composition starts.</summary>
        public event EventHandler? CompositionStarted;

        /// <summary>Occurs when IME composition completes.</summary>
        public event EventHandler? CompositionCompleted;

        /// <summary>Occurs when focus is removed from the text context.</summary>
        public event EventHandler? FocusRemoved;

        /// <summary>Occurs when a platform command is received (e.g. AppKit selectors like "deleteBackward:").</summary>
        public event EventHandler<CoreTextCommandReceivedEventArgs>? CommandReceived;

        // ----- Lifecycle (called by the host application) -----

        /// <summary>
        /// Attach this context to the given native window handle so the platform
        /// adapter can start listening for IME events.
        /// </summary>
        /// <param name="windowHandle">The native window handle (HWND, NSWindow*, X11 window id).</param>
        /// <param name="displayHandle">The native display handle (X11 Display* on Linux, 0 on other platforms).</param>
        /// <returns><c>true</c> if the adapter attached successfully.</returns>
        public bool Attach(nint windowHandle, nint displayHandle = 0) => _adapter.Attach(windowHandle, displayHandle, this);

        /// <summary>
        /// Notify the platform that the caret rectangle has changed so the IME
        /// candidate window can be repositioned.
        /// </summary>
        public void NotifyCaretRectChanged(double x, double y, double width, double height, double scale = 1.0)
            => _adapter.NotifyCaretRectChanged(x, y, width, height, scale);

        /// <summary>Notify the platform that this context has received keyboard focus.</summary>
        public void NotifyFocusEnter() => _adapter.NotifyFocusEnter();

        /// <summary>Notify the platform that this context has lost keyboard focus.</summary>
        public void NotifyFocusLeave() => _adapter.NotifyFocusLeave();

        /// <summary>
        /// Forward a key event to the platform IME for processing.
        /// Returns <c>true</c> if the IME consumed the key (caller should suppress normal handling).
        /// </summary>
        /// <param name="virtualKey">The virtual key code (cast from <c>Windows.System.VirtualKey</c>).</param>
        /// <param name="shiftPressed">Whether the Shift modifier is active.</param>
        /// <param name="controlPressed">Whether the Control modifier is active.</param>
        /// <param name="unicodeKey">Optional Unicode character for keys that map to VirtualKey.None.</param>
        public bool ProcessKeyEvent(int virtualKey, bool shiftPressed, bool controlPressed, char? unicodeKey = null)
            => _adapter.ProcessKeyEvent(virtualKey, shiftPressed, controlPressed, unicodeKey);

        /// <inheritdoc />
        public void Dispose() => _adapter.Dispose();

        // ----- Internal raise helpers (called by adapters) -----

        /// <summary>Raise the <see cref="TextRequested"/> event.</summary>
        public void RaiseTextRequested(CoreTextTextRequestedEventArgs e) => TextRequested?.Invoke(this, e);

        /// <summary>Raise the <see cref="TextUpdating"/> event.</summary>
        public void RaiseTextUpdating(CoreTextTextUpdatingEventArgs e) => TextUpdating?.Invoke(this, e);

        /// <summary>Raise the <see cref="SelectionRequested"/> event.</summary>
        public void RaiseSelectionRequested(CoreTextSelectionRequestedEventArgs e) => SelectionRequested?.Invoke(this, e);

        /// <summary>Raise the <see cref="SelectionUpdating"/> event.</summary>
        public void RaiseSelectionUpdating(CoreTextSelectionUpdatingEventArgs e) => SelectionUpdating?.Invoke(this, e);

        /// <summary>Raise the <see cref="LayoutRequested"/> event.</summary>
        public void RaiseLayoutRequested(CoreTextLayoutRequestedEventArgs e) => LayoutRequested?.Invoke(this, e);

        /// <summary>Raise the <see cref="CompositionStarted"/> event.</summary>
        public void RaiseCompositionStarted() => CompositionStarted?.Invoke(this, EventArgs.Empty);

        /// <summary>Raise the <see cref="CompositionCompleted"/> event.</summary>
        public void RaiseCompositionCompleted() => CompositionCompleted?.Invoke(this, EventArgs.Empty);

        /// <summary>Raise the <see cref="FocusRemoved"/> event.</summary>
        public void RaiseFocusRemoved() => FocusRemoved?.Invoke(this, EventArgs.Empty);

        /// <summary>Raise the <see cref="CommandReceived"/> event.</summary>
        public void RaiseCommandReceived(CoreTextCommandReceivedEventArgs e) => CommandReceived?.Invoke(this, e);
    }
}
