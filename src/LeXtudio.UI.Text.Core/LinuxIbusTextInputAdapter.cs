using System;
using System.Runtime.InteropServices;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Ubuntu/Linux adapter based on IBus over D-Bus.
    /// Attaches to an IBus input context, forwards focus/caret updates,
    /// and translates IBus signals into <see cref="CoreTextEditContext"/> events.
    /// </summary>
    internal sealed class LinuxIbusTextInputAdapter : IPlatformTextInputAdapter
    {
        private const uint IbusCapPreeditText = 1u << 0;
        private const uint IbusCapFocus = 1u << 3;

        private static readonly bool s_debug =
            string.Equals(Environment.GetEnvironmentVariable("LEXTUDIO_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

        private LinuxIBusConnection? _ibus;
        private CoreTextEditContext? _context;
        private bool _disposed;
        private bool _isComposing;
        private nint _x11Display;
        private nint _x11Window;

        [DllImport("libX11.so.6")]
        private static extern bool XTranslateCoordinates(
            nint display, nint src_w, nint dest_w,
            int src_x, int src_y,
            out int dest_x_return, out int dest_y_return,
            out nint child_return);

        [DllImport("libX11.so.6")]
        private static extern nint XDefaultRootWindow(nint display);

        /// <inheritdoc />
        public bool Attach(nint windowHandle, nint displayHandle, CoreTextEditContext context)
        {
            _context = context;
            _x11Window = windowHandle;
            _x11Display = displayHandle;

            _ibus = LinuxIBusConnection.TryConnect();
            if (_ibus == null || !_ibus.IsConnected)
            {
                Log("Attach failed: IBus is unavailable.");
                return false;
            }

            _ibus.SetCapabilities(IbusCapPreeditText | IbusCapFocus);

            Log("Attach succeeded: Linux IBus adapter active.");
            return true;
        }

        /// <inheritdoc />
        public void NotifyCaretRectChanged(double x, double y, double width, double height, double scale)
        {
            if (_ibus == null)
            {
                return;
            }

            int screenX = (int)(x * scale);
            int screenY = (int)(y * scale);
            int w = Math.Max(1, (int)(width * scale));
            int h = Math.Max(1, (int)(height * scale));

            // Convert window-relative to screen-absolute for IBus candidate window placement.
            if (_x11Display != nint.Zero && _x11Window != nint.Zero)
            {
                try
                {
                    nint root = XDefaultRootWindow(_x11Display);
                    if (XTranslateCoordinates(_x11Display, _x11Window, root,
                            screenX, screenY, out int destX, out int destY, out _))
                    {
                        screenX = destX;
                        screenY = destY;
                    }
                }
                catch
                {
                }
            }

            _ibus.SetCursorLocation(screenX, screenY, w, h);
        }

        /// <inheritdoc />
        public void NotifyFocusEnter() => _ibus?.FocusIn();

        /// <inheritdoc />
        public void NotifyFocusLeave() => _ibus?.FocusOut();

        /// <inheritdoc />
        public bool ProcessKeyEvent(int virtualKey, bool shiftPressed, bool controlPressed, char? unicodeKey = null)
        {
            if (_ibus == null || !_ibus.IsConnected)
            {
                return false;
            }

            // Control-key shortcuts are handled by the editor, not the IME.
            if (controlPressed)
            {
                return false;
            }

            uint keyval = X11KeyHelper.ConvertToX11Keysym(virtualKey, shiftPressed);

            // For OEM keys (VirtualKey.None on Skia/Linux), the keysym equals the
            // Unicode codepoint for printable ASCII characters.
            if (keyval == 0 && unicodeKey.HasValue && unicodeKey.Value >= 0x20 && unicodeKey.Value < 0x7F)
            {
                keyval = (uint)unicodeKey.Value;
            }

            if (keyval == 0)
            {
                return false; // unknown key — let the editor handle it
            }

            uint state = X11KeyHelper.GetX11ModifierState(shiftPressed, controlPressed);

            // Drain any pending signals before sending the key event.
            DrainSignals();

            var (handled, _) = _ibus.ProcessKeyEvent(keyval, 0, state);
            Log($"ProcessKeyEvent keyval=0x{keyval:X} state=0x{state:X} -> handled={handled}");

            // Drain signals that IBus may have produced in response.
            // Signals may arrive shortly after the method reply, so use a
            // blocking read with a short timeout for the first attempt.
            DrainSignals(blockTimeoutMs: 50);

            return handled;
        }

        private void DrainSignals(int blockTimeoutMs = 0)
        {
            if (_ibus?.Connection == null)
            {
                return;
            }

            for (int i = 0; i < 16; i++)
            {
                LinuxDBusMessage? msg;
                if (i == 0 && blockTimeoutMs > 0)
                {
                    // First iteration: use a blocking receive to wait for signals
                    // that may arrive shortly after the ProcessKeyEvent reply.
                    msg = _ibus.Connection.TryReceiveOne(blockTimeoutMs);
                    if (msg != null && msg.Type == LinuxDBusConstants.Signal)
                    {
                        Log($"DrainSignals: blocking receive got signal: {msg.Interface}.{msg.Member}");
                    }
                    else if (msg != null)
                    {
                        Log($"DrainSignals: blocking receive got non-signal type={msg.Type} interface={msg.Interface} member={msg.Member}");
                        continue;
                    }
                    else
                    {
                        // Check the signal queue in case SendAndWaitReply queued it.
                        msg = _ibus.Connection.Poll();
                        if (msg != null)
                        {
                            Log($"DrainSignals: poll got queued msg type={msg.Type} interface={msg.Interface} member={msg.Member}");
                        }
                    }
                }
                else
                {
                    msg = _ibus.Connection.Poll();
                    if (msg != null)
                    {
                        Log($"DrainSignals: poll got msg type={msg.Type} interface={msg.Interface} member={msg.Member}");
                    }
                }

                if (msg == null)
                {
                    break;
                }

                if (msg.Type != LinuxDBusConstants.Signal || msg.Interface != "org.freedesktop.IBus.InputContext")
                {
                    continue;
                }

                switch (msg.Member)
                {
                    case "CommitText":
                        HandleCommitText(msg);
                        break;
                    case "UpdatePreeditText":
                        HandleUpdatePreeditText(msg);
                        break;
                    case "HidePreeditText":
                        _context?.RaiseCompositionCompleted();
                        break;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _ibus?.Dispose();
            _ibus = null;
            _context = null;
        }

        private void HandleCommitText(LinuxDBusMessage msg)
        {
            Log($"HandleCommitText: body.Length={msg.Body.Length}");
            if (msg.Body.Length == 0)
            {
                return;
            }

            try
            {
                var reader = new LinuxDBusReader(msg.Body, 0);
                string? text = reader.ReadIBusText();
                Log($"HandleCommitText: text='{text}'");
                if (!string.IsNullOrEmpty(text))
                {
                    _isComposing = false;
                    var request = new CoreTextTextRequest(text);
                    _context?.RaiseTextRequested(new CoreTextTextRequestedEventArgs(request));
                    _context?.RaiseCompositionCompleted();
                }
            }
            catch (Exception ex)
            {
                Log($"HandleCommitText parse error: {ex.Message}");
            }
        }

        private void HandleUpdatePreeditText(LinuxDBusMessage msg)
        {
            Log($"HandleUpdatePreeditText: body.Length={msg.Body.Length}");
            if (msg.Body.Length == 0)
            {
                return;
            }

            try
            {
                var reader = new LinuxDBusReader(msg.Body, 0);
                string? text = reader.ReadIBusText();
                uint _ = reader.ReadUInt32();
                bool visible = reader.ReadBool();

                Log($"HandleUpdatePreeditText: text='{text}' visible={visible}");

                if (!visible || string.IsNullOrEmpty(text))
                {
                    if (_isComposing)
                    {
                        _isComposing = false;
                        _context?.RaiseCompositionCompleted();
                    }
                    return;
                }

                if (!_isComposing)
                {
                    _isComposing = true;
                    _context?.RaiseCompositionStarted();
                }
                _context?.RaiseTextUpdating(new CoreTextTextUpdatingEventArgs(text));
            }
            catch (Exception ex)
            {
                Log($"HandleUpdatePreeditText parse error: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            if (s_debug)
            {
                Console.Error.WriteLine($"[LeXtudio Linux IME] {message}");
            }
        }
    }
}
