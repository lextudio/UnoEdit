using System;
using System.Threading;

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
        private Thread? _pollThread;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        /// <inheritdoc />
        public bool Attach(nint windowHandle, CoreTextEditContext context)
        {
            _context = context;
            _ibus = LinuxIBusConnection.TryConnect();
            if (_ibus == null || !_ibus.IsConnected)
            {
                Log("Attach failed: IBus is unavailable.");
                return false;
            }

            _ibus.SetCapabilities(IbusCapPreeditText | IbusCapFocus);
            _cts = new CancellationTokenSource();
            _pollThread = new Thread(() => PollLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "LeXtudio-LinuxIBus-Poll",
            };
            _pollThread.Start();

            Log("Attach succeeded: Linux IBus adapter active.");
            return true;
        }

        /// <inheritdoc />
        public void NotifyCaretRectChanged(double x, double y, double width, double height)
        {
            _ibus?.SetCursorLocation((int)x, (int)y, Math.Max(1, (int)width), Math.Max(1, (int)height));
        }

        /// <inheritdoc />
        public void NotifyFocusEnter() => _ibus?.FocusIn();

        /// <inheritdoc />
        public void NotifyFocusLeave() => _ibus?.FocusOut();

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _cts?.Cancel();
                _pollThread?.Join(200);
            }
            catch
            {
            }

            _cts?.Dispose();
            _cts = null;
            _pollThread = null;

            _ibus?.Dispose();
            _ibus = null;
            _context = null;
        }

        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    LinuxDBusMessage? msg = _ibus?.Connection?.Poll();
                    if (msg == null)
                    {
                        Thread.Sleep(5);
                        continue;
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
                catch (Exception ex)
                {
                    Log($"PollLoop error: {ex.Message}");
                }
            }
        }

        private void HandleCommitText(LinuxDBusMessage msg)
        {
            if (msg.Body.Length == 0)
            {
                return;
            }

            try
            {
                var reader = new LinuxDBusReader(msg.Body, 0);
                string? text = reader.ReadIBusText();
                if (!string.IsNullOrEmpty(text))
                {
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

                if (!visible || string.IsNullOrEmpty(text))
                {
                    _context?.RaiseCompositionCompleted();
                    return;
                }

                _context?.RaiseCompositionStarted();
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
