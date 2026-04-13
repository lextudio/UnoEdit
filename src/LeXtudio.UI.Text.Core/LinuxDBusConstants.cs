namespace LeXtudio.UI.Text.Core;

internal static class LinuxDBusConstants
{
    internal const byte MethodCall = 1;
    internal const byte MethodReturn = 2;
    internal const byte Error = 3;
    internal const byte Signal = 4;

    internal const byte FieldPath = 1;
    internal const byte FieldInterface = 2;
    internal const byte FieldMember = 3;
    internal const byte FieldErrorName = 4;
    internal const byte FieldReplySerial = 5;
    internal const byte FieldDestination = 6;
    internal const byte FieldSender = 7;
    internal const byte FieldSignature = 8;

    internal const string BusName = "org.freedesktop.DBus";
    internal const string BusPath = "/org/freedesktop/DBus";
    internal const string BusInterface = "org.freedesktop.DBus";

    internal const byte LittleEndian = (byte)'l';
    internal const byte ProtocolVersion = 1;
}
