using NUnit.Framework;
using UnoEdit.Skia.Desktop.Controls;
namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextLineViewModelTests
{
    [Test]
    public void UpdateFrom_UpdatesLineNumberAndNumber_WhenViewportRowsAreReused()
    {
        var theme = TextEditorTheme.Dark;
        var original = new TextLineViewModel(
            10,
            "line 10",
            0,
            0,
            new Microsoft.UI.Xaml.Thickness(0),
            new Microsoft.UI.Xaml.Thickness(0),
            0,
            0,
            new Microsoft.UI.Xaml.Thickness(0),
            0,
            0,
            theme,
            showLineNumbers: true,
            gutterForegroundOverride: Microsoft.UI.Colors.Gray);

        var replacement = new TextLineViewModel(
            20,
            "line 20",
            0,
            0,
            new Microsoft.UI.Xaml.Thickness(0),
            new Microsoft.UI.Xaml.Thickness(0),
            0,
            0,
            new Microsoft.UI.Xaml.Thickness(0),
            0,
            0,
            theme,
            showLineNumbers: true,
            gutterForegroundOverride: Microsoft.UI.Colors.Gray);

        original.UpdateFrom(replacement);

        Assert.That(original.LineNumber, Is.EqualTo("20"));
        Assert.That(original.Number, Is.EqualTo("20"));
        Assert.That(original.Text, Is.EqualTo("line 20"));
    }
}
