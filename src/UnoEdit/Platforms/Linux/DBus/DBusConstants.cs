namespace UnoEdit.Skia.Desktop.Controls.Platform.Linux.DBus;

internal static class DBusConstants
{
    // Message types
    internal const byte MethodCall = 1;
    internal const byte MethodReturn = 2;
    internal const byte Error = 3;
    internal const byte Signal = 4;

    // Header field codes
    internal const byte FieldPath = 1;
    internal const byte FieldInterface = 2;
    internal const byte FieldMember = 3;
    internal const byte FieldErrorName = 4;
    internal const byte FieldReplySerial = 5;
    internal const byte FieldDestination = 6;
    internal const byte FieldSender = 7;
    internal const byte FieldSignature = 8;

    // Well-known names
    internal const string BusName = "org.freedesktop.DBus";
    internal const string BusPath = "/org/freedesktop/DBus";
    internal const string BusInterface = "org.freedesktop.DBus";

    // Endianness
    internal const byte LittleEndian = (byte)'l';

    // Protocol version
    internal const byte ProtocolVersion = 1;
}
