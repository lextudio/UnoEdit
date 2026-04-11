using System.Buffers.Binary;
using System.Text;

namespace UnoEdit.Skia.Desktop.Controls.Platform.Linux.DBus;

/// <summary>
/// Reads D-Bus wire format from a byte buffer (little-endian only).
/// Ported from MewUI's DBusReader with namespace adaptation.
/// </summary>
internal ref struct DBusReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    public DBusReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos = 0;
    }

    public DBusReader(byte[] data, int offset)
    {
        _data = data.AsSpan();
        _pos = offset;
    }

    public int Position => _pos;
    public int Remaining => _data.Length - _pos;

    public byte ReadByte() => _data[_pos++];

    public bool ReadBool()
    {
        Align(4);
        uint val = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val != 0;
    }

    public int ReadInt32()
    {
        Align(4);
        int val = BinaryPrimitives.ReadInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val;
    }

    public uint ReadUInt32()
    {
        Align(4);
        uint val = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        return val;
    }

    public string ReadString()
    {
        Align(4);
        uint len = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += 4;
        string val = Encoding.UTF8.GetString(_data.Slice(_pos, (int)len));
        _pos += (int)len + 1; // skip NUL
        return val;
    }

    public string ReadObjectPath() => ReadString();

    public string ReadSignature()
    {
        byte len = _data[_pos++];
        string val = Encoding.UTF8.GetString(_data.Slice(_pos, len));
        _pos += len + 1; // skip NUL
        return val;
    }

    public void Align(int alignment)
    {
        int pad = (alignment - (_pos % alignment)) % alignment;
        _pos += pad;
    }

    public void Skip(int bytes) => _pos += bytes;

    public void SkipValue(char sig)
    {
        switch (sig)
        {
            case 'y': _pos++; break;
            case 'b': Align(4); _pos += 4; break;
            case 'n': case 'q': Align(2); _pos += 2; break;
            case 'i': case 'u': Align(4); _pos += 4; break;
            case 'x': case 't': case 'd': Align(8); _pos += 8; break;
            case 's': case 'o': ReadString(); break;
            case 'g': ReadSignature(); break;
            case 'v': SkipVariant(); break;
            case 'a': SkipArray(); break;
            case '(': SkipStruct(); break;
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

    private void SkipStruct()
    {
        Align(8); // struct alignment; without full signature we can't skip safely
    }

    /// <summary>
    /// Reads an IBus serialized text variant.
    /// IBus format: VARIANT containing STRUCT (sa{sv}sv)
    ///   s: type name ("IBusText")
    ///   a{sv}: properties dict (usually empty)
    ///   s: the actual text string
    ///   v: attributes (IBusAttrList)
    /// </summary>
    public string? ReadIBusText()
    {
        try
        {
            ReadSignature(); // inner type signature of the variant
            Align(8);        // struct alignment

            string typeName = ReadString();
            if (typeName != "IBusText")
            {
                return null;
            }

            // Skip a{sv} properties dict
            Align(4);
            uint dictLen = ReadUInt32();
            Align(8); // dict entries align to 8 (even for empty arrays)
            _pos += (int)dictLen;

            return ReadString(); // the actual text
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a complete D-Bus message from a byte buffer.
    /// Returns null if not enough data; sets <paramref name="consumed"/> to bytes consumed.
    /// </summary>
    public static DBusMessage? TryParse(byte[] buffer, int offset, int length, out int consumed)
    {
        consumed = 0;
        if (length < 16)
        {
            return null;
        }

        var r = new DBusReader(buffer, offset);

        byte endian = r.ReadByte();
        if (endian != DBusConstants.LittleEndian)
        {
            return null;
        }

        byte msgType = r.ReadByte();
        byte flags = r.ReadByte();
        _ = r.ReadByte(); // protocol version
        uint bodyLength = r.ReadUInt32();
        uint serial = r.ReadUInt32();
        uint headerFieldsLength = r.ReadUInt32();

        int headerEnd = 12 + 4 + (int)headerFieldsLength;
        int paddedHeaderEnd = (headerEnd + 7) & ~7; // align to 8
        int totalSize = paddedHeaderEnd + (int)bodyLength;

        if (length < totalSize)
        {
            return null;
        }

        string? path = null, iface = null, member = null, errorName = null;
        string? dest = null, sender = null, signature = null;
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
                case DBusConstants.FieldPath:
                    path = r.ReadObjectPath();
                    break;
                case DBusConstants.FieldInterface:
                    iface = r.ReadString();
                    break;
                case DBusConstants.FieldMember:
                    member = r.ReadString();
                    break;
                case DBusConstants.FieldErrorName:
                    errorName = r.ReadString();
                    break;
                case DBusConstants.FieldReplySerial:
                    replySerial = r.ReadUInt32();
                    break;
                case DBusConstants.FieldDestination:
                    dest = r.ReadString();
                    break;
                case DBusConstants.FieldSender:
                    sender = r.ReadString();
                    break;
                case DBusConstants.FieldSignature:
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
        return new DBusMessage
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
