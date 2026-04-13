using System;
using System.IO;

namespace LeXtudio.UI.Text.Core;

internal sealed class LinuxIBusConnection : IDisposable
{
    private static readonly bool s_debug =
        string.Equals(Environment.GetEnvironmentVariable("LEXTUDIO_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

    private LinuxDBusConnection? _conn;
    private string? _inputContextPath;
    private bool _disposed;

    public bool IsConnected => _conn?.IsConnected == true && _inputContextPath != null;
    internal LinuxDBusConnection? Connection => _conn;

    public static LinuxIBusConnection? TryConnect()
    {
        var ibus = new LinuxIBusConnection();
        ibus._conn = TryConnectIbusSocket() ?? TryConnectViaSessionBus();
        if (ibus._conn == null)
        {
            ibus.Dispose();
            return null;
        }

        if (!ibus.CreateInputContext())
        {
            ibus.Dispose();
            return null;
        }

        ibus.SubscribeSignals();
        return ibus;
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
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
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
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            "SetCapabilities",
            "u",
            bw => bw.WriteUInt32(caps));

        _conn.Send(msg);
    }

    private static LinuxDBusConnection? TryConnectIbusSocket()
    {
        string? ibusAddress = Environment.GetEnvironmentVariable("IBUS_ADDRESS");
        if (string.IsNullOrEmpty(ibusAddress))
        {
            ibusAddress = ReadIbusAddress();
        }

        if (string.IsNullOrEmpty(ibusAddress))
        {
            return null;
        }

        return LinuxDBusConnection.TryConnect(ibusAddress);
    }

    private static LinuxDBusConnection? TryConnectViaSessionBus()
    {
        var sessionConn = LinuxDBusConnection.TryConnectSession();
        if (sessionConn == null)
        {
            return null;
        }

        uint serial = sessionConn.NextSerial();
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            "/org/freedesktop/IBus",
            "org.freedesktop.IBus",
            "GetAddress");

        LinuxDBusMessage? reply = sessionConn.SendAndWaitReply(msg, serial, 2000);
        if (reply != null && reply.Type == LinuxDBusConstants.MethodReturn && reply.Body.Length > 0)
        {
            try
            {
                var r = new LinuxDBusReader(reply.Body, 0);
                string address = r.ReadString();
                sessionConn.Dispose();
                return LinuxDBusConnection.TryConnect(address);
            }
            catch
            {
            }
        }

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
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            "/org/freedesktop/IBus",
            "org.freedesktop.IBus",
            "CreateInputContext",
            "s",
            bw => bw.WriteString("LeXtudio.TextCore"));

        LinuxDBusMessage? reply = _conn.SendAndWaitReply(msg, serial, 3000);
        if (reply == null || reply.Type != LinuxDBusConstants.MethodReturn || reply.Body.Length == 0)
        {
            return false;
        }

        try
        {
            var r = new LinuxDBusReader(reply.Body, 0);
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

        string[] signals = ["CommitText", "UpdatePreeditText", "HidePreeditText"];
        foreach (string signal in signals)
        {
            uint serial = _conn.NextSerial();
            string rule = $"type='signal',interface='org.freedesktop.IBus.InputContext',member='{signal}',path='{_inputContextPath}'";
            byte[] msg = LinuxDBusWriter.BuildAddMatch(serial, rule);
            _conn.Send(msg);
        }
    }

    private void CallVoidMethod(string method)
    {
        if (_conn == null || _inputContextPath == null)
        {
            return;
        }

        uint serial = _conn.NextSerial();
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
            serial,
            "org.freedesktop.IBus",
            _inputContextPath,
            "org.freedesktop.IBus.InputContext",
            method);

        _conn.Send(msg);
    }

    private static string? ReadIbusAddress()
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

            string machineId = string.Empty;
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
                string[] files = Directory.GetFiles(ibusDir, $"{machineId}*");
                if (files.Length == 0)
                {
                    return null;
                }

                filePath = files[0];
            }

            foreach (string line in File.ReadAllLines(filePath))
            {
                if (line.StartsWith("IBUS_ADDRESS=", StringComparison.Ordinal))
                {
                    return line[13..];
                }
            }
        }
        catch (Exception ex)
        {
            Log($"ReadIbusAddress error: {ex.Message}");
        }

        return null;
    }

    private static void Log(string message)
    {
        if (s_debug)
        {
            Console.Error.WriteLine($"[LeXtudio IBus] {message}");
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
                byte[] msg = LinuxDBusWriter.BuildMethodCall(
                    serial,
                    "org.freedesktop.IBus",
                    _inputContextPath,
                    "org.freedesktop.IBus.InputContext",
                    "Destroy");

                _conn.Send(msg);
            }
        }
        catch
        {
        }

        _conn?.Dispose();
        _conn = null;
    }
}
