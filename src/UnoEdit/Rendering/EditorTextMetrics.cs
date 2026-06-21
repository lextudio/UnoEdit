using Microsoft.UI.Xaml.Media;

namespace UnoEdit.Skia.Desktop.Controls;

internal static class EditorTextMetrics
{
    public const double FontSize = 13d;

    /// <summary>
    /// Default editor family: Cascadia Code (an open-source monospaced code font). Apps that bundle
    /// the font as an asset can override <c>TextEditor.EditorFontFamily</c> with an
    /// <c>ms-appx:///…#Cascadia Code</c> reference for deterministic cross-platform rendering; by
    /// the bare name it resolves from the system when installed.
    /// </summary>
    public const string EditorFontFamilySource = "Cascadia Code";

    public static FontFamily CreateFontFamily()
    {
        return new FontFamily(EditorFontFamilySource);
    }
}
