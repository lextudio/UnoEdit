using UnoEdit.Skia.Desktop.Controls.Platform.Linux.DBus;

namespace UnoEdit.Skia.Desktop.Controls.Platform.Linux.IBus;

/// <summary>
/// Manages the D-Bus connection to the IBus daemon and the input context lifecycle.
/// Ported from MewUI's IBusConnection with namespace adaptation.
/// </summary>
internal sealed class IBusConnection : IDisposable
{
    private static readonly bool s_debug =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

    private DBusConnection? _conn;
    private string? _inputContextPath;
    private bool _disposed;

    public bool IsConnected => _conn?.IsConnected == true && _inputContextPath != null;

    internal DBusConnection? Connection => _conn;

    /// <summary>
    /// Attempts to connect to IBus and create an input context.
    /// Returns null if IBus is unavailable.
    /// </summary>
    public static IBusConnection? TryConnect()
    {
        var ibus = new IBusConnection();

        // Try IBus-specific socket first, fall back to session bus.
        ibus._conn = TryConnectIBusSocket();
        ibus._conn ??= TryConnectViaSessionBus();

        if (ibus._conn == null)
        {
            Log("Cannot connect to IBus");
            ibus.Dispose();
            return null;
        }

        if (!ibus.CreateInputContext())
        {
            Log("Failed to create IBus input context");
            ibus.Dispose();
            return null;
        }

        ibus.SubscribeSignals();
        Log($"Connected; IC path: {ibus._inputContextPath}");
        return ibus;
    }

    private static DBusConnection? TryConnectIBusSocket()
    {
        string? ibusAddress = null;
        try { ibusAddress = Environment.GetEnvironmentVariable("IBUS_ADDRESS"); } catch { }

        if (string.IsNullOrEmpty(ibusAddress))
        {
            ibusAddress = ReadIBusAddress();
        }

        if (string.IsNullOrEmpty(ibusAddress))
        {
            return null;
        }

        Log($"Trying IBus socket: {ibusAddress}");
        return DBusConnection.TryConnect(ibusAddress);
    }

    private static DBusConnection? TryConnectViaSessionBus()
    {
        Log("Trying IBus via session bus");
        var sessionConn = DBusConnection.TryConnectSession();
        if (sessionConn == null)
        {
            return null;
        }

        uint serial = sessionConn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            "/org/freedesktop/IBus",
            "org.freedesktop.IBus",
            "GetAddress");

        var reply = sessionConn.SendAndWaitReply(msg, serial, 2000);
        if (reply != null && reply.Type == DBusConstants.MethodReturn && reply.Body.Length > 0)
        {
            try
            {
                var r = new DBusReader(reply.Body, 0);
                string ibusAddr = r.ReadString();
                Log($"IBus address from session bus: {ibusAddr}");
                sessionConn.Dispose();
                return DBusConnection.TryConnect(ibusAddr);
            }
            catch { }
        }

        Log("IBus not found on session bus");
        sessionConn.Dispose();
        return null;
    }

    private bool CreateInputContext()
    {
        if (_conn == null)
        {
            return false;
        }

        uint serial = _conn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            "/org/freedesktop/IBus",
            "org.freedesktop.IBus",
            "CreateInputContext",
            "s",
            bw => bw.WriteString("UnoEdit"));

        var reply = _conn.SendAndWaitReply(msg, serial, 3000);
        if (reply == null || reply.Type != DBusConstants.MethodReturn || reply.Body.Length == 0)
        {
            return false;
        }

        try
        {
            var r = new DBusReader(reply.Body, 0);
            _inputContextPath = r.ReadObjectPath();
            return !string.IsNullOrEmpty(_inputContextPath);
        }
        catch
        {
            return false;
        }
    }

    private void SubscribeSignals()
    {
        if (_conn == null || _inputContextPath == null)
        {
            return;
        }

        string[] signals = ["CommitText", "UpdatePreeditText", "HidePreeditText", "ForwardKeyEvent"];
        foreach (var signal in signals)
        {
            uint serial = _conn.NextSerial();
            string rule = $"type='signal',interface='org.freedesktop.IBus.InputContext',member='{signal}',path='{_inputContextPath}'";
            var msg = DBusWriter.BuildAddMatch(serial, rule);
            _conn.Send(msg);
        }
    }

    /// <summary>
    /// Calls ProcessKeyEvent on the IBus input context (synchronous, blocking).
    /// Returns (handled, forward).
    /// </summary>
    public (bool handled, bool forward) ProcessKeyEvent(uint keyval, uint keycode, uint state)
    {
        if (_conn == null || _inputContextPath == null)
        {
            return (false, true);
        }

        uint serial = _conn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            "ProcessKeyEvent",
            "uuu",
            bw =>
            {
                bw.WriteUInt32(keyval);
                bw.WriteUInt32(keycode);
                bw.WriteUInt32(state);
            });

        var reply = _conn.SendAndWaitReply(msg, serial, 200); // tight timeout for key latency
        if (reply == null || reply.Type != DBusConstants.MethodReturn || reply.Body.Length < 4)
        {
            return (false, true);
        }

        try
        {
            var r = new DBusReader(reply.Body, 0);
            bool handled = r.ReadBool();
            return (handled, !handled);
        }
        catch
        {
            return (false, true);
        }
    }

    public void FocusIn() => CallVoidMethod("FocusIn");
    public void FocusOut() => CallVoidMethod("FocusOut");
    public void Reset() => CallVoidMethod("Reset");

    public void SetCursorLocation(int x, int y, int w, int h)
    {
        if (_conn == null || _inputContextPath == null)
        {
            return;
        }

        uint serial = _conn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            "SetCursorLocation",
            "iiii",
            bw =>
            {
                bw.WriteInt32(x);
                bw.WriteInt32(y);
                bw.WriteInt32(w);
                bw.WriteInt32(h);
            });

        _conn.Send(msg);
    }

    public void SetCapabilities(uint caps)
    {
        if (_conn == null || _inputContextPath == null)
        {
            return;
        }

        uint serial = _conn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            "SetCapabilities",
            "u",
            bw => bw.WriteUInt32(caps));

        _conn.Send(msg);
    }

    private void CallVoidMethod(string method)
    {
        if (_conn == null || _inputContextPath == null)
        {
            return;
        }

        uint serial = _conn.NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            method);

        _conn.Send(msg);
    }

    private static string? ReadIBusAddress()
    {
        try
        {
            string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome))
            {
                string? home = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(home))
                {
                    return null;
                }

                configHome = Path.Combine(home, ".config");
            }

            string ibusDir = Path.Combine(configHome, "ibus", "bus");
            if (!Directory.Exists(ibusDir))
            {
                return null;
            }

            string machineId = "";
            if (File.Exists("/etc/machine-id"))
            {
                machineId = File.ReadAllText("/etc/machine-id").Trim();
            }
            else if (File.Exists("/var/lib/dbus/machine-id"))
            {
                machineId = File.ReadAllText("/var/lib/dbus/machine-id").Trim();
            }

            if (string.IsNullOrEmpty(machineId))
            {
                return null;
            }

            string? display = Environment.GetEnvironmentVariable("DISPLAY");
            if (string.IsNullOrEmpty(display))
            {
                return null;
            }

            string displayNum = "0";
            int colonIdx = display.IndexOf(':');
            if (colonIdx >= 0)
            {
                string rest = display[(colonIdx + 1)..];
                int dotIdx = rest.IndexOf('.');
                displayNum = dotIdx >= 0 ? rest[..dotIdx] : rest;
            }

            string filePath = Path.Combine(ibusDir, $"{machineId}-unix-{displayNum}");
            if (!File.Exists(filePath))
            {
                var files = Directory.GetFiles(ibusDir, $"{machineId}*");
                if (files.Length == 0)
                {
                    return null;
                }

                filePath = files[0];
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (line.StartsWith("IBUS_ADDRESS=", StringComparison.Ordinal))
                {
                    return line[13..];
                }
            }
        }
        catch (Exception ex)
        {
            Log($"ReadIBusAddress error: {ex.Message}");
        }

        return null;
    }

    private static void Log(string message)
    {
        if (s_debug)
        {
            Console.Error.WriteLine($"[UnoEdit IBus] {message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (_conn != null && _inputContextPath != null)
            {
                uint serial = _conn.NextSerial();
                var msg = DBusWriter.BuildMethodCall(
                    serial,
                    "org.freedesktop.IBus",
                    _inputContextPath,
                    "org.freedesktop.IBus.InputContext",
                    "Destroy");
                _conn.Send(msg);
            }
        }
        catch { }

        _conn?.Dispose();
        _conn = null;
    }
}
