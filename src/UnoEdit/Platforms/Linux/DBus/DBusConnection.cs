using System.Net.Sockets;
using System.Text;

namespace UnoEdit.Skia.Desktop.Controls.Platform.Linux.DBus;

/// <summary>
/// Minimal D-Bus connection over Unix domain socket.
/// Supports EXTERNAL authentication and blocking message send/receive.
/// Ported from MewUI's DBusConnection with namespace adaptation.
/// </summary>
internal sealed class DBusConnection : IDisposable
{
    private static readonly bool s_debug =
        string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_LINUX_IME"), "1", StringComparison.Ordinal);

    private Socket? _socket;
    private readonly byte[] _recvBuffer = new byte[8192];
    private int _recvBufferUsed;
    private uint _nextSerial = 1;
    private bool _disposed;
    private readonly Queue<DBusMessage> _signalQueue = new();

    public bool IsConnected => _socket?.Connected == true;

    public uint NextSerial() => _nextSerial++;

    /// <summary>
    /// Connects to a D-Bus server at the given address (unix:path=... or unix:abstract=...).
    /// Returns null on failure.
    /// </summary>
    public static DBusConnection? TryConnect(string address)
    {
        string? socketPath = ParseUnixPath(address);
        if (socketPath == null)
        {
            Log($"Cannot parse address: {address}");
            return null;
        }

        var conn = new DBusConnection();
        try
        {
            conn._socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            conn._socket.Connect(new UnixDomainSocketEndPoint(socketPath));

            if (!conn.Authenticate())
            {
                Log($"Auth failed for {socketPath}");
                conn.Dispose();
                return null;
            }

            if (!conn.Hello())
            {
                Log($"Hello failed for {socketPath}");
                conn.Dispose();
                return null;
            }

            Log($"Connected to {socketPath}");
            return conn;
        }
        catch (Exception ex)
        {
            Log($"Connect failed ({socketPath}): {ex.Message}");
            conn.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Connects to the session D-Bus.
    /// Returns null on failure.
    /// </summary>
    public static DBusConnection? TryConnectSession()
    {
        string? address = GetSessionBusAddress();
        if (address == null)
        {
            Log("No session bus address found");
            return null;
        }

        string? socketPath = ParseUnixPath(address);
        if (socketPath == null)
        {
            Log($"Cannot parse bus address: {address}");
            return null;
        }

        var conn = new DBusConnection();
        try
        {
            conn._socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            conn._socket.Connect(new UnixDomainSocketEndPoint(socketPath));

            if (!conn.Authenticate())
            {
                Log("D-Bus authentication failed");
                conn.Dispose();
                return null;
            }

            if (!conn.Hello())
            {
                Log("D-Bus Hello failed");
                conn.Dispose();
                return null;
            }

            Log($"Connected to session bus at {socketPath}");
            return conn;
        }
        catch (Exception ex)
        {
            Log($"Failed to connect: {ex.Message}");
            conn.Dispose();
            return null;
        }
    }

    /// <summary>Sends raw bytes to the bus.</summary>
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

    /// <summary>
    /// Sends a method call and blocks until the reply arrives or timeout elapses.
    /// Signals received while waiting are queued for later retrieval via Poll().
    /// </summary>
    public DBusMessage? SendAndWaitReply(byte[] data, uint serial, int timeoutMs = 3000)
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

            if ((msg.Type == DBusConstants.MethodReturn || msg.Type == DBusConstants.Error)
                && msg.ReplySerial == serial)
            {
                return msg;
            }

            if (msg.Type == DBusConstants.Signal)
            {
                _signalQueue.Enqueue(msg);
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to receive one message, with a timeout (ms).
    /// Returns null if no complete message is available within the timeout.
    /// </summary>
    public DBusMessage? TryReceiveOne(int timeoutMs = 0)
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

    /// <summary>
    /// Non-blocking poll: returns any pending message, or null.
    /// Checks the signal queue (signals buffered during SendAndWaitReply) first.
    /// </summary>
    public DBusMessage? Poll()
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

    private DBusMessage? TryParseFromBuffer()
    {
        if (_recvBufferUsed < 16)
        {
            return null;
        }

        var msg = DBusReader.TryParse(_recvBuffer, 0, _recvBufferUsed, out int consumed);
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
            _socket.Send(new byte[] { 0 }); // credential byte

            string uid = GetUid();
            string hexUid = BitConverter.ToString(Encoding.ASCII.GetBytes(uid)).Replace("-", "");
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
        catch (Exception ex)
        {
            Log($"Auth error: {ex.Message}");
            return false;
        }
    }

    private bool Hello()
    {
        uint serial = NextSerial();
        var msg = DBusWriter.BuildMethodCall(
            serial,
            DBusConstants.BusName,
            DBusConstants.BusPath,
            DBusConstants.BusInterface,
            "Hello");

        var reply = SendAndWaitReply(msg, serial);
        return reply != null && reply.Type == DBusConstants.MethodReturn;
    }

    private static string GetUid()
    {
        try
        {
            string status = File.ReadAllText("/proc/self/status");
            foreach (var line in status.Split('\n'))
            {
                if (line.StartsWith("Uid:", StringComparison.Ordinal))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }
        }
        catch { }

        return "1000"; // fallback
    }

    private static string? GetSessionBusAddress()
    {
        string? addr = null;
        try { addr = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"); } catch { }
        if (!string.IsNullOrEmpty(addr))
        {
            return addr;
        }

        string? xdgRuntime = null;
        try { xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"); } catch { }
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
        foreach (var part in address.Split(';'))
        {
            if (!part.StartsWith("unix:", StringComparison.Ordinal))
            {
                continue;
            }

            var kvPairs = part[5..].Split(',');
            foreach (var kv in kvPairs)
            {
                if (kv.StartsWith("path=", StringComparison.Ordinal))
                {
                    return kv[5..];
                }

                if (kv.StartsWith("abstract=", StringComparison.Ordinal))
                {
                    return "\0" + kv[9..]; // abstract socket uses NUL prefix
                }
            }
        }

        return null;
    }

    private static void Log(string message)
    {
        if (s_debug)
        {
            Console.Error.WriteLine($"[UnoEdit DBus] {message}");
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
