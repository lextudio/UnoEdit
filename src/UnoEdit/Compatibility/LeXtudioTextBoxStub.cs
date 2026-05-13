using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// Stub that satisfies the LeXtudio.UI.Controls.TextBox API surface used by SearchPanel.xaml
// on the WinUI target, where the real CoreText-bridged implementation is unavailable.
namespace LeXtudio.UI.Controls;

public class TextBox : Microsoft.UI.Xaml.Controls.TextBox
{
    public static readonly DependencyProperty PlaceholderForegroundProperty =
        DependencyProperty.Register(
            nameof(PlaceholderForeground),
            typeof(Brush),
            typeof(TextBox),
            new PropertyMetadata(null));

    public Brush? PlaceholderForeground
    {
        get => (Brush?)GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }
}
