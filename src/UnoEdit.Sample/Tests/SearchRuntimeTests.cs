using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class SearchRuntimeTests
{
    [TestMethod]
    public async Task OpenSearchPanel_SeedsSingleLineSelection()
    {
        var document = new TextDocument("alpha beta alpha");
        var editor = new TextEditor { Document = document };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.SetSelection(0, 5);
        editor.OpenSearchPanel();
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.IsTrue(editor.SearchPanel.IsOpen);
        Assert.AreEqual("alpha", editor.SearchPanel.SearchPattern);
    }

    [TestMethod]
    public async Task FindNext_SelectsNextMatch()
    {
        var document = new TextDocument("alpha beta alpha gamma alpha");
        var editor = new TextEditor { Document = document };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.OpenSearchPanel();
        editor.SearchPanel.SearchPattern = "alpha";
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.FindNext();
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(0, editor.SelectionStartOffset);
        Assert.AreEqual(5, editor.SelectionEndOffset);

        editor.FindNext();
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(11, editor.SelectionStartOffset);
        Assert.AreEqual(16, editor.SelectionEndOffset);
    }
}
