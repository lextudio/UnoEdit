using System;
using System.Buffers.Binary;
using System.Text;

namespace LeXtudio.UI.Text.Core;

internal sealed class LinuxDBusWriter
{
    private byte[] _buffer;
    private int _pos;

    public LinuxDBusWriter(int initialCapacity = 256)
    {
        _buffer = new byte[initialCapacity];
    }

    public int Position => _pos;

    public byte[] ToArray()
    {
        var result = new byte[_pos];
        Buffer.BlockCopy(_buffer, 0, result, 0, _pos);
        return result;
    }

    public void Align(int alignment)
    {
        int pad = (alignment - (_pos % alignment)) % alignment;
        EnsureCapacity(pad);
        for (int i = 0; i < pad; i++)
        {
            _buffer[_pos++] = 0;
        }
    }

    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_pos++] = value;
    }

    public void WriteUInt32(uint value)
    {
        Align(4);
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 4;
    }

    public void WriteInt32(int value)
    {
        Align(4);
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 4;
    }

    public void WriteString(string value)
    {
        Align(4);
        int byteCount = Encoding.UTF8.GetByteCount(value);
        EnsureCapacity(4 + byteCount + 1);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_pos), (uint)byteCount);
        _pos += 4;
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_pos));
        _pos += byteCount;
        _buffer[_pos++] = 0;
    }

    public void WriteObjectPath(string value) => WriteString(value);

    public void WriteSignature(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        EnsureCapacity(1 + byteCount + 1);
        _buffer[_pos++] = (byte)byteCount;
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_pos));
        _pos += byteCount;
        _buffer[_pos++] = 0;
    }

    public void WriteRaw(byte[] data)
    {
        EnsureCapacity(data.Length);
        Buffer.BlockCopy(data, 0, _buffer, _pos, data.Length);
        _pos += data.Length;
    }

    private void EnsureCapacity(int additional)
    {
        int required = _pos + additional;
        if (required <= _buffer.Length)
        {
            return;
        }

        int newSize = Math.Max(_buffer.Length * 2, required);
        var newBuf = new byte[newSize];
        Buffer.BlockCopy(_buffer, 0, newBuf, 0, _pos);
        _buffer = newBuf;
    }

    private static void WriteHeaderField(LinuxDBusWriter w, byte code, string sig, string value)
    {
        w.Align(8);
        w.WriteByte(code);
        w.WriteSignature(sig);
        if (sig == "o")
        {
            w.WriteObjectPath(value);
        }
        else
        {
            w.WriteString(value);
        }
    }

    private static void WriteHeaderFieldSignature(LinuxDBusWriter w, string sig)
    {
        w.Align(8);
        w.WriteByte(LinuxDBusConstants.FieldSignature);
        w.WriteSignature("g");
        w.WriteSignature(sig);
    }

    public static byte[] BuildMethodCall(
        uint serial,
        string destination,
        string path,
        string @interface,
        string member,
        string? signature = null,
        Action<LinuxDBusWriter>? writeBody = null)
    {
        byte[] body = [];
        if (writeBody != null && signature != null)
        {
            var bw = new LinuxDBusWriter(128);
            writeBody(bw);
            body = bw.ToArray();
        }

        var w = new LinuxDBusWriter(128 + body.Length);
        w.WriteByte(LinuxDBusConstants.LittleEndian);
        w.WriteByte(LinuxDBusConstants.MethodCall);
        w.WriteByte(0);
        w.WriteByte(LinuxDBusConstants.ProtocolVersion);
        w.WriteUInt32((uint)body.Length);
        w.WriteUInt32(serial);

        var hw = new LinuxDBusWriter(128);
        WriteHeaderField(hw, LinuxDBusConstants.FieldPath, "o", path);
        WriteHeaderField(hw, LinuxDBusConstants.FieldInterface, "s", @interface);
        WriteHeaderField(hw, LinuxDBusConstants.FieldMember, "s", member);
        WriteHeaderField(hw, LinuxDBusConstants.FieldDestination, "s", destination);
        if (signature != null)
        {
            WriteHeaderFieldSignature(hw, signature);
        }

        var headerBytes = hw.ToArray();
        w.WriteUInt32((uint)headerBytes.Length);
        w.WriteRaw(headerBytes);
        w.Align(8);

        if (body.Length > 0)
        {
            w.WriteRaw(body);
        }

        return w.ToArray();
    }

    public static byte[] BuildAddMatch(uint serial, string rule)
        => BuildMethodCall(
            serial,
            LinuxDBusConstants.BusName,
            LinuxDBusConstants.BusPath,
            LinuxDBusConstants.BusInterface,
            "AddMatch",
            "s",
            bw => bw.WriteString(rule));
}
