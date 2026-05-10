using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media;

namespace UnoEdit.Skia.Desktop.Controls;

internal static class EditorTextMetrics
{
    public const double FontSize = 13d;

    public static FontFamily CreateFontFamily()
    {
        string name;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            name = "Menlo";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            name = "DejaVu Sans Mono";
        }
        else
        {
            name = "Consolas";
        }

        return new FontFamily(name);
    }
}