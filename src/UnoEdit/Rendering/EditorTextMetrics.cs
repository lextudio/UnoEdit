using Microsoft.UI.Xaml.Media;

namespace UnoEdit.Skia.Desktop.Controls;

internal static class EditorTextMetrics
{
    public const double FontSize = 13d;

    public static FontFamily CreateFontFamily()
    {
        return new FontFamily("Open Sans");
    }
}
