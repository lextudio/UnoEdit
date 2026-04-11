using UnoEdit.Skia.Desktop.Controls.Platform.Linux.DBus;
using UnoEdit.Skia.Desktop.Controls.Platform.Linux.IBus;
using Windows.Foundation;

namespace UnoEdit.Skia.Desktop.Controls.Platform.Linux;

/// <summary>
/// Linux IME bridge via IBus D-Bus.
/// Processes key events synchronously by forwarding them to IBus, then draining
/// IBus signals (CommitText, UpdatePreeditText) on the calling thread.
/// </summary>
internal sealed class LinuxImeBridge : IDisposable
{
    private const uint IBUS_CAP_PREEDIT_TEXT = 1u << 0;
    private const uint IBUS_CAP_FOCUS = 1u << 3;
    private const uint IBUS_RELEASE_MASK = 1u << 30;

    private static readonly bool s_debug =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

    private IBusConnection? _ibus;
    private bool _disposed;
    public bool IsAvailable => _ibus?.IsConnected == true;

    // Invoked synchronously on the caller's thread (UI thread).
    public event Action<string>? TextCommitted;
    public event Action<string>? PreeditUpdated;
    public event Action? PreeditEnded;

    private LinuxImeBridge(IBusConnection ibus)
    {
        _ibus = ibus;
    }

    public static LinuxImeBridge? TryCreate()
    {
        var conn = IBusConnection.TryConnect();
        if (conn == null)
        {
            return null;
        }

        var bridge = new LinuxImeBridge(conn);
        conn.SetCapabilities(IBUS_CAP_PREEDIT_TEXT | IBUS_CAP_FOCUS);
        Log("Created Linux IBus bridge");
        return bridge;
    }

    public void FocusIn() => _ibus?.FocusIn();
    public void FocusOut() => _ibus?.FocusOut();
    public void Reset() => _ibus?.Reset();

    public void UpdateCursorLocation(Rect rect)
    {
        _ibus?.SetCursorLocation(
            (int)rect.X,
            (int)rect.Y,
            (int)rect.Width,
            (int)rect.Height);
    }

    /// <summary>
    /// Forwards a key event to IBus and drains pending signals.
    /// Called synchronously on the UI thread; blocks at most ~200 ms (IBus timeout).
    /// Returns <c>true</c> if IBus consumed the key (caller should suppress normal handling).
    /// </summary>
    public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
    {
        if (_ibus == null || !_ibus.IsConnected)
        {
            return false;
        }

        DrainSignals();

        var (handled, _) = _ibus.ProcessKeyEvent(keyval, keycode, state);
        Log($"ProcessKeyEvent keyval=0x{keyval:X} state=0x{state:X} -> handled={handled}");

        DrainSignals();

        return handled;
    }

    /// <summary>
    /// Forwards a key-release to IBus (without blocking for reply in normal use).
    /// </summary>
    public void ProcessKeyRelease(uint keyval, uint keycode, uint state)
    {
        if (_ibus == null || !_ibus.IsConnected)
        {
            return;
        }

        _ibus.ProcessKeyEvent(keyval, keycode, state | IBUS_RELEASE_MASK);
    }

    // -----------------------------------------------------------------------
    // Signal drain
    // -----------------------------------------------------------------------

    private void DrainSignals()
    {
        if (_ibus?.Connection == null)
        {
            return;
        }

        for (int i = 0; i < 16; i++)
        {
            var msg = _ibus.Connection.Poll();
            if (msg == null)
            {
                break;
            }

            if (msg.Type != DBusConstants.Signal)
            {
                continue;
            }

            ProcessSignal(msg);
        }
    }

    private void ProcessSignal(DBusMessage msg)
    {
        if (msg.Interface != "org.freedesktop.IBus.InputContext")
        {
            return;
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
                Log("HidePreeditText");
                PreeditEnded?.Invoke();
                break;
        }
    }

    private void HandleCommitText(DBusMessage msg)
    {
        if (msg.Body.Length == 0)
        {
            return;
        }

        try
        {
            var r = new DBusReader(msg.Body, 0);
            string? text = r.ReadIBusText();
            if (!string.IsNullOrEmpty(text))
            {
                Log($"CommitText: '{text}'");
                TextCommitted?.Invoke(text);
            }
        }
        catch (Exception ex)
        {
            Log($"CommitText parse error: {ex.Message}");
        }
    }

    private void HandleUpdatePreeditText(DBusMessage msg)
    {
        if (msg.Body.Length == 0)
        {
            return;
        }

        try
        {
            var r = new DBusReader(msg.Body, 0);
            string? text = r.ReadIBusText();
            uint cursorPos = r.ReadUInt32();
            bool visible = r.ReadBool();

            Log($"UpdatePreeditText: '{text}' cursor={cursorPos} visible={visible}");

            if (!visible || string.IsNullOrEmpty(text))
            {
                PreeditEnded?.Invoke();
            }
            else
            {
                PreeditUpdated?.Invoke(text);
            }
        }
        catch (Exception ex)
        {
            Log($"UpdatePreeditText parse error: {ex.Message}");
        }
    }

    private static void Log(string message)
    {
        if (s_debug)
        {
            Console.Error.WriteLine($"[UnoEdit Linux IME] {message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ibus?.Dispose();
        _ibus = null;
    }
}
