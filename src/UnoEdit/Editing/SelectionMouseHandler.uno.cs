using Microsoft.UI.Xaml.Input;

namespace UnoEdit.Skia.Desktop.Controls;

internal static class SelectionMouseHandler
{
    public static bool HandlePointerPressed(TextView textView, PointerRoutedEventArgs e)
    {
        return textView.HandlePointerPressedCore(e);
    }

    public static bool HandlePointerMoved(TextView textView, PointerRoutedEventArgs e)
    {
        return textView.HandlePointerMovedCore(e);
    }

    public static bool HandlePointerReleased(TextView textView, PointerRoutedEventArgs e)
    {
        return textView.HandlePointerReleasedCore(e);
    }

    public static bool HandleFoldGlyphPointerPressed(TextView textView, object sender)
    {
        return textView.HandleFoldGlyphPointerPressedCore(sender);
    }
}
