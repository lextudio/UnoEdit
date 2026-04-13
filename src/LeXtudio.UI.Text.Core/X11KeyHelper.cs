namespace LeXtudio.UI.Text.Core;

/// <summary>
/// Maps virtual key codes and modifier flags to X11 keysyms and modifier state.
/// Used by the Linux IBus adapter to forward key events over D-Bus.
/// </summary>
internal static class X11KeyHelper
{
    // Virtual key values matching Windows.System.VirtualKey enum.
    private const int VK_Back = 8;
    private const int VK_Tab = 9;
    private const int VK_Enter = 13;
    private const int VK_Escape = 27;
    private const int VK_Space = 32;
    private const int VK_PageUp = 33;
    private const int VK_PageDown = 34;
    private const int VK_End = 35;
    private const int VK_Home = 36;
    private const int VK_Left = 37;
    private const int VK_Up = 38;
    private const int VK_Right = 39;
    private const int VK_Down = 40;
    private const int VK_Delete = 46;
    private const int VK_Number0 = 48;
    private const int VK_Number9 = 57;
    private const int VK_A = 65;
    private const int VK_Z = 90;

    /// <summary>
    /// Convert a virtual key code (cast from <c>Windows.System.VirtualKey</c>) to an X11 keysym.
    /// </summary>
    public static uint ConvertToX11Keysym(int virtualKey, bool shiftPressed)
    {
        if (virtualKey >= VK_A && virtualKey <= VK_Z)
        {
            int offset = virtualKey - VK_A;
            return shiftPressed ? (uint)(0x41 + offset) : (uint)(0x61 + offset);
        }

        if (virtualKey >= VK_Number0 && virtualKey <= VK_Number9)
        {
            int digit = virtualKey - VK_Number0;
            if (shiftPressed)
            {
                // US keyboard shifted digits: ) ! @ # $ % ^ & * (
                uint[] shifted = [0x29, 0x21, 0x40, 0x23, 0x24, 0x25, 0x5E, 0x26, 0x2A, 0x28];
                return digit < shifted.Length ? shifted[digit] : 0;
            }

            return (uint)(0x30 + digit);
        }

        return virtualKey switch
        {
            VK_Back => 0xFF08u,      // BackSpace
            VK_Tab => 0xFF09u,       // Tab
            VK_Enter => 0xFF0Du,     // Return
            VK_Escape => 0xFF1Bu,    // Escape
            VK_Space => 0x0020u,     // space
            VK_PageUp => 0xFF55u,
            VK_PageDown => 0xFF56u,
            VK_End => 0xFF57u,
            VK_Home => 0xFF50u,
            VK_Left => 0xFF51u,
            VK_Up => 0xFF52u,
            VK_Right => 0xFF53u,
            VK_Down => 0xFF54u,
            VK_Delete => 0xFFFFu,
            186 => shiftPressed ? 0x3Au : 0x3Bu, // : or ;
            187 => shiftPressed ? 0x2Bu : 0x3Du, // + or =
            188 => shiftPressed ? 0x3Cu : 0x2Cu, // < or ,
            189 => shiftPressed ? 0x5Fu : 0x2Du, // _ or -
            190 => shiftPressed ? 0x3Eu : 0x2Eu, // > or .
            191 => shiftPressed ? 0x3Fu : 0x2Fu, // ? or /
            219 => shiftPressed ? 0x7Bu : 0x5Bu, // { or [
            220 => shiftPressed ? 0x7Cu : 0x5Cu, // | or \
            221 => shiftPressed ? 0x7Du : 0x5Du, // } or ]
            222 => shiftPressed ? 0x22u : 0x27u, // " or '
            _ => 0u
        };
    }

    /// <summary>
    /// Build the X11 modifier state mask from boolean modifier flags.
    /// </summary>
    public static uint GetX11ModifierState(bool shiftPressed, bool controlPressed)
    {
        uint state = 0;
        if (shiftPressed)
        {
            state |= 0x0001; // ShiftMask
        }

        if (controlPressed)
        {
            state |= 0x0004; // ControlMask
        }

        return state;
    }
}
