using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class TextLineViewModelRuntimeTests
{
    [TestMethod]
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
            gutterForegroundOverride: Microsoft.UI.Colors.Gray);

        original.UpdateFrom(replacement);

        Assert.AreEqual("20", original.LineNumber);
        Assert.AreEqual("20", original.Number);
        Assert.AreEqual("line 20", original.Text);
    }
}
