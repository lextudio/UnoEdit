namespace UnoEdit.Skia.Desktop.Controls;

internal static class EditingCommandHandler
{
    public static async Task<bool> HandleKeyDownAsync(TextView textView, Windows.System.VirtualKey key, bool controlPressed, bool extendSelection)
    {
        bool handled = key switch
        {
            Windows.System.VirtualKey.A when controlPressed => textView.SelectAll(),
            Windows.System.VirtualKey.C when controlPressed => textView.CopySelection(),
            Windows.System.VirtualKey.Y when controlPressed => textView.Redo(),
            Windows.System.VirtualKey.Z when controlPressed && extendSelection => textView.Redo(),
            Windows.System.VirtualKey.Z when controlPressed => textView.Undo(),
            Windows.System.VirtualKey.X when controlPressed => textView.CutSelection(),
            Windows.System.VirtualKey.Insert when !controlPressed => ToggleOverstrike(textView),
            Windows.System.VirtualKey.Back when controlPressed => textView.DeleteWord(backward: true),
            Windows.System.VirtualKey.Delete when controlPressed => textView.DeleteWord(backward: false),
            Windows.System.VirtualKey.Back => textView.Backspace(),
            Windows.System.VirtualKey.Delete => textView.Delete(),
            Windows.System.VirtualKey.Enter => textView.InsertText(Environment.NewLine, raiseTextInputEvents: true),
            Windows.System.VirtualKey.Tab => textView.InsertText("\t", raiseTextInputEvents: true),
            _ when !controlPressed => textView.InsertPrintableKey(key, extendSelection),
            _ => false
        };

        if (!handled && controlPressed && key == Windows.System.VirtualKey.V)
        {
            handled = await textView.PasteAsync();
        }

        return handled;
    }

    private static bool ToggleOverstrike(TextView textView)
    {
        textView.OverstrikeMode = !textView.OverstrikeMode;
        return true;
    }
}
