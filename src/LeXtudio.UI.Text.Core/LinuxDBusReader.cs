using System;
using System.Buffers.Binary;
using System.Text;

namespace LeXtudio.UI.Text.Core;

internal ref struct LinuxDBusReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    public LinuxDBusReader(byte[] data, int offset)
    {
        _data = data.AsSpan();
        _pos = offset;
    }

    public int Position => _pos;

    public byte ReadByte() => _data[_pos++];

    public bool ReadBool()
    {
        Align(4);
        uint val = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val != 0;
    }

    public uint ReadUInt32()
    {
        Align(4);
        uint val = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val;
    }

    public int ReadInt32()
    {
        Align(4);
        int val = BinaryPrimitives.ReadInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val;
    }

    public string ReadString()
    {
        Align(4);
        uint len = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        string val = Encoding.UTF8.GetString(_data.Slice(_pos, (int)len));
        _pos += (int)len + 1;
        return val;
    }

    public string ReadObjectPath() => ReadString();

    public string ReadSignature()
    {
        byte len = _data[_pos++];
        string val = Encoding.UTF8.GetString(_data.Slice(_pos, len));
        _pos += len + 1;
        return val;
    }

    public void Align(int alignment)
    {
        int pad = (alignment - (_pos % alignment)) % alignment;
        _pos += pad;
    }

    public void SkipValue(char sig)
    {
        switch (sig)
        {
            case 'y': _pos++; break;
            case 'b': Align(4); _pos += 4; break;
            case 'n':
            case 'q': Align(2); _pos += 2; break;
            case 'i':
            case 'u': Align(4); _pos += 4; break;
            case 'x':
            case 't':
            case 'd': Align(8); _pos += 8; break;
            case 's':
            case 'o': ReadString(); break;
            case 'g': ReadSignature(); break;
            case 'v': SkipVariant(); break;
            case 'a': SkipArray(); break;
            case '(': Align(8); break;
        }
    }

    public string? ReadIBusText()
    {
        try
        {
            ReadSignature();
            Align(8);
            string typeName = ReadString();
            if (typeName != "IBusText")
            {
                return null;
            }

            Align(4);
            uint dictLen = ReadUInt32();
            Align(8);
            _pos += (int)dictLen;
            return ReadString();
        }
        catch
        {
            return null;
        }
    }

    private void SkipVariant()
    {
        string sig = ReadSignature();
        foreach (char c in sig)
        {
            SkipValue(c);
        }
    }

    private void SkipArray()
    {
        Align(4);
        uint arrayLen = ReadUInt32();
        _pos += (int)arrayLen;
    }

    public static LinuxDBusMessage? TryParse(byte[] buffer, int offset, int length, out int consumed)
    {
        consumed = 0;
        if (length < 16)
        {
            return null;
        }

        var r = new LinuxDBusReader(buffer, offset);
        byte endian = r.ReadByte();
        if (endian != LinuxDBusConstants.LittleEndian)
        {
            return null;
        }

        byte msgType = r.ReadByte();
        byte flags = r.ReadByte();
        _ = r.ReadByte();
        uint bodyLength = r.ReadUInt32();
        uint serial = r.ReadUInt32();
        uint headerFieldsLength = r.ReadUInt32();

        int headerEnd = 12 + 4 + (int)headerFieldsLength;
        int paddedHeaderEnd = (headerEnd + 7) & ~7;
        int totalSize = paddedHeaderEnd + (int)bodyLength;
        if (length < totalSize)
        {
            return null;
        }

        string? path = null;
        string? iface = null;
        string? member = null;
        string? errorName = null;
        string? dest = null;
        string? sender = null;
        string? signature = null;
        uint replySerial = 0;

        int fieldsEnd = r.Position + (int)headerFieldsLength;
        while (r.Position < fieldsEnd)
        {
            r.Align(8);
            if (r.Position >= fieldsEnd)
            {
                break;
            }

            byte code = r.ReadByte();
            string varSig = r.ReadSignature();
            switch (code)
            {
                case LinuxDBusConstants.FieldPath:
                    path = r.ReadObjectPath();
                    break;
                case LinuxDBusConstants.FieldInterface:
                    iface = r.ReadString();
                    break;
                case LinuxDBusConstants.FieldMember:
                    member = r.ReadString();
                    break;
                case LinuxDBusConstants.FieldErrorName:
                    errorName = r.ReadString();
                    break;
                case LinuxDBusConstants.FieldReplySerial:
                    replySerial = r.ReadUInt32();
                    break;
                case LinuxDBusConstants.FieldDestination:
                    dest = r.ReadString();
                    break;
                case LinuxDBusConstants.FieldSender:
                    sender = r.ReadString();
                    break;
                case LinuxDBusConstants.FieldSignature:
                    signature = r.ReadSignature();
                    break;
                default:
                    foreach (char c in varSig)
                    {
                        r.SkipValue(c);
                    }

                    break;
            }
        }

        byte[] body = new byte[bodyLength];
        Buffer.BlockCopy(buffer, offset + paddedHeaderEnd, body, 0, (int)bodyLength);

        consumed = totalSize;
        return new LinuxDBusMessage
        {
            Type = msgType,
            Flags = flags,
            Serial = serial,
            ReplySerial = replySerial,
            Path = path,
            Interface = iface,
            Member = member,
            ErrorName = errorName,
            Destination = dest,
            Sender = sender,
            Signature = signature,
            Body = body,
        };
    }
}
