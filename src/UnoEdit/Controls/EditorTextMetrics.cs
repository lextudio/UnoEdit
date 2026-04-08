using Microsoft.UI.Xaml.Media;

namespace UnoEdit.Skia.Desktop.Controls;

internal static class EditorTextMetrics
{
#if __UNO_SKIA_MACOS__
    public const string FontFamilyName = "Menlo";
#else
    public const string FontFamilyName = "Consolas";
#endif

    public const double FontSize = 13d;

    public static FontFamily CreateFontFamily() => new(FontFamilyName);
}