using Microsoft.UI.Xaml;

namespace UnoEdit.Skia.Desktop;

internal static class WindowExtensions
{
    // Provide a minimal fallback so builds don't fail when the original
    // extension (from the library) is not available. Keep this no-op
    // to avoid platform-specific complications here.
    public static void SetWindowIcon(this Window? window)
    {
        // Intentionally no-op. Platforms that need to set an icon should
        // implement this in a platform-specific partial or provide the
        // original extension in the shared library.
    }
}
