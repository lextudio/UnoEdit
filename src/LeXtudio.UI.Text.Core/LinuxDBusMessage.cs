namespace LeXtudio.UI.Text.Core;

internal sealed class LinuxDBusMessage
{
    public byte Type { get; init; }
    public byte Flags { get; init; }
    public uint Serial { get; init; }
    public uint ReplySerial { get; init; }

    public string? Path { get; init; }
    public string? Interface { get; init; }
    public string? Member { get; init; }
    public string? ErrorName { get; init; }
    public string? Destination { get; init; }
    public string? Sender { get; init; }
    public string? Signature { get; init; }

    public byte[] Body { get; init; } = [];
}
