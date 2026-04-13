using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace LeXtudio.UI.Text.Core;

internal sealed class LinuxDBusConnection : IDisposable
{
    private static readonly bool s_debug =
        string.Equals(Environment.GetEnvironmentVariable("LEXTUDIO_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

    private Socket? _socket;
    private readonly byte[] _recvBuffer = new byte[8192];
    private int _recvBufferUsed;
    private uint _nextSerial = 1;
    private bool _disposed;
    private readonly Queue<LinuxDBusMessage> _signalQueue = new();

    public bool IsConnected => _socket?.Connected == true;

    public uint NextSerial() => _nextSerial++;

    public static LinuxDBusConnection? TryConnect(string address)
    {
        string? socketPath = ParseUnixPath(address);
        if (socketPath == null)
        {
            return null;
        }

        var conn = new LinuxDBusConnection();
        try
        {
            conn._socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            conn._socket.Connect(new UnixDomainSocketEndPoint(socketPath));

            if (!conn.Authenticate() || !conn.Hello())
            {
                conn.Dispose();
                return null;
            }

            return conn;
        }
        catch (Exception ex)
        {
            Log($"Connect failed ({socketPath}): {ex.Message}");
            conn.Dispose();
            return null;
        }
    }

    public static LinuxDBusConnection? TryConnectSession()
    {
        string? address = GetSessionBusAddress();
        if (address == null)
        {
            return null;
        }

        return TryConnect(address);
    }

    public bool Send(byte[] data)
    {
        if (_socket == null || !_socket.Connected)
        {
            return false;
        }

        try
        {
            int sent = 0;
            while (sent < data.Length)
            {
                int n = _socket.Send(data, sent, data.Length - sent, SocketFlags.None);
                if (n <= 0)
                {
                    return false;
                }

                sent += n;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public LinuxDBusMessage? SendAndWaitReply(byte[] data, uint serial, int timeoutMs = 3000)
    {
        if (!Send(data))
        {
            return null;
        }

        long deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            var msg = TryReceiveOne(Math.Max(1, (int)(deadline - Environment.TickCount64)));
            if (msg == null)
            {
                continue;
            }

            if ((msg.Type == LinuxDBusConstants.MethodReturn || msg.Type == LinuxDBusConstants.Error)
                && msg.ReplySerial == serial)
            {
                return msg;
            }

            if (msg.Type == LinuxDBusConstants.Signal)
            {
                _signalQueue.Enqueue(msg);
            }
        }

        return null;
    }

    public LinuxDBusMessage? TryReceiveOne(int timeoutMs = 0)
    {
        var msg = TryParseFromBuffer();
        if (msg != null)
        {
            return msg;
        }

        if (_socket == null)
        {
            return null;
        }

        try
        {
            _socket.ReceiveTimeout = timeoutMs > 0 ? timeoutMs : 1;
            int available = _recvBuffer.Length - _recvBufferUsed;
            if (available <= 0)
            {
                return null;
            }

            int n = _socket.Receive(_recvBuffer, _recvBufferUsed, available, SocketFlags.None);
            if (n <= 0)
            {
                return null;
            }

            _recvBufferUsed += n;
        }
        catch (SocketException)
        {
            return null;
        }

        return TryParseFromBuffer();
    }

    public LinuxDBusMessage? Poll()
    {
        if (_signalQueue.Count > 0)
        {
            return _signalQueue.Dequeue();
        }

        var msg = TryParseFromBuffer();
        if (msg != null)
        {
            return msg;
        }

        if (_socket == null)
        {
            return null;
        }

        try
        {
            if (_socket.Available <= 0)
            {
                return null;
            }

            int available = _recvBuffer.Length - _recvBufferUsed;
            if (available <= 0)
            {
                return null;
            }

            _socket.ReceiveTimeout = 1;
            int n = _socket.Receive(_recvBuffer, _recvBufferUsed, available, SocketFlags.None);
            if (n <= 0)
            {
                return null;
            }

            _recvBufferUsed += n;
        }
        catch (SocketException)
        {
            return null;
        }

        return TryParseFromBuffer();
    }

    private LinuxDBusMessage? TryParseFromBuffer()
    {
        if (_recvBufferUsed < 16)
        {
            return null;
        }

        var msg = LinuxDBusReader.TryParse(_recvBuffer, 0, _recvBufferUsed, out int consumed);
        if (msg == null)
        {
            return null;
        }

        int remaining = _recvBufferUsed - consumed;
        if (remaining > 0)
        {
            Buffer.BlockCopy(_recvBuffer, consumed, _recvBuffer, 0, remaining);
        }

        _recvBufferUsed = remaining;
        return msg;
    }

    private bool Authenticate()
    {
        if (_socket == null)
        {
            return false;
        }

        try
        {
            _socket.Send([0]);

            string uid = GetUid();
            string hexUid = BitConverter.ToString(Encoding.ASCII.GetBytes(uid)).Replace("-", string.Empty, StringComparison.Ordinal);
            string authCmd = $"AUTH EXTERNAL {hexUid}\r\n";
            _socket.Send(Encoding.ASCII.GetBytes(authCmd));

            byte[] buf = new byte[256];
            int n = _socket.Receive(buf);
            string response = Encoding.ASCII.GetString(buf, 0, n);
            if (!response.StartsWith("OK ", StringComparison.Ordinal))
            {
                Log($"Auth response: {response.TrimEnd()}");
                return false;
            }

            _socket.Send(Encoding.ASCII.GetBytes("BEGIN\r\n"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool Hello()
    {
        uint serial = NextSerial();
        byte[] msg = LinuxDBusWriter.BuildMethodCall(
            serial,
            LinuxDBusConstants.BusName,
            LinuxDBusConstants.BusPath,
            LinuxDBusConstants.BusInterface,
            "Hello");

        LinuxDBusMessage? reply = SendAndWaitReply(msg, serial);
        return reply != null && reply.Type == LinuxDBusConstants.MethodReturn;
    }

    private static string GetUid()
    {
        try
        {
            string status = File.ReadAllText("/proc/self/status");
            foreach (string line in status.Split('\n'))
            {
                if (!line.StartsWith("Uid:", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return parts[1].Trim();
                }
            }
        }
        catch
        {
        }

        return "1000";
    }

    private static string? GetSessionBusAddress()
    {
        string? addr = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
        if (!string.IsNullOrEmpty(addr))
        {
            return addr;
        }

        string? xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrEmpty(xdgRuntime))
        {
            string busPath = Path.Combine(xdgRuntime, "bus");
            if (File.Exists(busPath))
            {
                return $"unix:path={busPath}";
            }
        }

        return null;
    }

    private static string? ParseUnixPath(string address)
    {
        foreach (string part in address.Split(';'))
        {
            if (!part.StartsWith("unix:", StringComparison.Ordinal))
            {
                continue;
            }

            string[] kvPairs = part[5..].Split(',');
            foreach (string kv in kvPairs)
            {
                if (kv.StartsWith("path=", StringComparison.Ordinal))
                {
                    return kv[5..];
                }

                if (kv.StartsWith("abstract=", StringComparison.Ordinal))
                {
                    return "\0" + kv[9..];
                }
            }
        }

        return null;
    }

    private static void Log(string message)
    {
        if (s_debug)
        {
            Console.Error.WriteLine($"[LeXtudio DBus] {message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket?.Dispose(); } catch { }
        _socket = null;
    }
}
