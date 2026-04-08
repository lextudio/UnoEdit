// ClipboardRuntimeTests — UI thread tests for cut/copy/paste behavior.
// Runs headlessly when UNO_RUNTIME_TESTS_RUN_TESTS env var is set.

using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using Windows.ApplicationModel.DataTransfer;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class ClipboardRuntimeTests
{
    [TestMethod]
    public async Task CopySelection_PutsTextOnClipboard()
    {
        var doc = new TextDocument("alpha\nbeta\ngamma");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        // select "alpha"
        editor.SetSelection(0, 5);
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);

        var copyMethod = textView.GetType().GetMethod("CopySelection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(copyMethod, "CopySelection method should exist.");
        bool copyResult = (bool)copyMethod.Invoke(textView, null)!;
        Assert.IsTrue(copyResult, "CopySelection should return true.");

        var pkg = Clipboard.GetContent();
        string clipboardText = await pkg.GetTextAsync();
        Assert.AreEqual("alpha", clipboardText);
    }

    [TestMethod]
    public async Task CutSelection_RemovesTextAndPutsOnClipboard()
    {
        var doc = new TextDocument("hello world");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.SetSelection(0, 5); // "hello"
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var cutMethod = textView.GetType().GetMethod("CutSelection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(cutMethod, "CutSelection method should exist.");
        bool cutResult = (bool)cutMethod.Invoke(textView, null)!;
        Assert.IsTrue(cutResult, "CutSelection should return true.");

        Assert.AreEqual(" world", doc.Text);

        var pkg = Clipboard.GetContent();
        string clipboardText = await pkg.GetTextAsync();
        Assert.AreEqual("hello", clipboardText);
    }

    [TestMethod]
    public async Task Paste_InsertsClipboardTextAtCaret()
    {
        var doc = new TextDocument("abc");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.CurrentOffset = 3; // end
        await UnitTestsUIContentHelper.WaitForIdle();

        var dp = new DataPackage();
        dp.SetText("XYZ");
        Clipboard.SetContent(dp);

        var textView = GetTextViewInstance(editor);
        var pasteMethod = textView.GetType().GetMethod("PasteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(pasteMethod, "PasteAsync method should exist.");
        var task = (Task<bool>)pasteMethod.Invoke(textView, null)!;
        bool pasted = await task;
        Assert.IsTrue(pasted, "PasteAsync should return true.");
        Assert.AreEqual("abcXYZ", doc.Text);
    }

    private static TextView GetTextViewInstance(TextEditor editor)
        => editor.TextArea.TextView;
}
