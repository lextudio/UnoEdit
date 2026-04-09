namespace UnoEdit.Skia.Desktop.Controls;

internal static class CaretNavigationCommandHandler
{
    public static bool HandleKeyDown(TextView textView, Windows.System.VirtualKey key, bool controlPressed, bool extendSelection)
    {
        if (controlPressed && key == Windows.System.VirtualKey.M)
        {
            return textView.ToggleFoldAtCaret();
        }

        return key switch
        {
            Windows.System.VirtualKey.Left when controlPressed => textView.MoveWordBoundary(backward: true, extendSelection),
            Windows.System.VirtualKey.Right when controlPressed => textView.MoveWordBoundary(backward: false, extendSelection),
            Windows.System.VirtualKey.Left => textView.MoveHorizontal(-1, extendSelection),
            Windows.System.VirtualKey.Right => textView.MoveHorizontal(1, extendSelection),
            Windows.System.VirtualKey.Up => textView.MoveVertical(-1, extendSelection),
            Windows.System.VirtualKey.Down => textView.MoveVertical(1, extendSelection),
            Windows.System.VirtualKey.PageUp => textView.MovePageVertical(-1, extendSelection),
            Windows.System.VirtualKey.PageDown => textView.MovePageVertical(1, extendSelection),
            Windows.System.VirtualKey.Home when controlPressed => textView.MoveToDocumentBoundary(true, extendSelection),
            Windows.System.VirtualKey.End when controlPressed => textView.MoveToDocumentBoundary(false, extendSelection),
            Windows.System.VirtualKey.Home => textView.MoveToLineBoundary(true, extendSelection),
            Windows.System.VirtualKey.End => textView.MoveToLineBoundary(false, extendSelection),
            _ => false
        };
    }
}
