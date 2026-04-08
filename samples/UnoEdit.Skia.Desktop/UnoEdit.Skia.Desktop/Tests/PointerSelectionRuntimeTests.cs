// PointerSelectionRuntimeTests — UI thread tests for pointer drag selection.
// Runs headlessly when UNO_RUNTIME_TESTS_RUN_TESTS env var is set.

using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class PointerSelectionRuntimeTests
{
    [TestMethod]
    public async Task DragSelect_Forwards_ExpandsSelection()
    {
        var doc = new TextDocument("first line\nsecond line\nthird line");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var tvType = textView.GetType();

        // Private method: void UpdateCaretAndSelection(int targetOffset, bool extendSelection)
        var updateMethod = tvType.GetMethod("UpdateCaretAndSelection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(updateMethod, "UpdateCaretAndSelection should exist");

        // Anchor at start of document (offset 0)
        int anchorOffset = doc.GetOffset(1, 1);
        updateMethod.Invoke(textView, new object[] { anchorOffset, false });
        await UnitTestsUIContentHelper.WaitForIdle();

        // Extend selection forward to after "first"
        int extendOffset = anchorOffset + 5; // select first 5 chars
        updateMethod.Invoke(textView, new object[] { extendOffset, true });
        await UnitTestsUIContentHelper.WaitForIdle();

        int selectionStart = (int)tvType.GetProperty("SelectionStartOffset", BindingFlags.Instance | BindingFlags.Public)!.GetValue(textView)!;
        int selectionEnd = (int)tvType.GetProperty("SelectionEndOffset", BindingFlags.Instance | BindingFlags.Public)!.GetValue(textView)!;

        Assert.AreEqual(anchorOffset, selectionStart, "Selection start should equal anchor offset");
        Assert.AreEqual(extendOffset, selectionEnd, "Selection end should equal extended offset");
    }

    [TestMethod]
    public async Task DragSelect_Backwards_ExpandsSelection()
    {
        var doc = new TextDocument("alpha beta gamma\nnext line");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var tvType = textView.GetType();
        var updateMethod = tvType.GetMethod("UpdateCaretAndSelection", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(updateMethod, "UpdateCaretAndSelection should exist");

        // Anchor at offset 10
        int anchorOffset = 10;
        updateMethod.Invoke(textView, new object[] { anchorOffset, false });
        await UnitTestsUIContentHelper.WaitForIdle();

        // Move pointer backward to offset 3 (extend selection backwards)
        int extendOffset = 3;
        updateMethod.Invoke(textView, new object[] { extendOffset, true });
        await UnitTestsUIContentHelper.WaitForIdle();

        int selectionStart = (int)tvType.GetProperty("SelectionStartOffset", BindingFlags.Instance | BindingFlags.Public)!.GetValue(textView)!;
        int selectionEnd = (int)tvType.GetProperty("SelectionEndOffset", BindingFlags.Instance | BindingFlags.Public)!.GetValue(textView)!;

        Assert.AreEqual(extendOffset, selectionStart, "Selection start should be the smaller offset");
        Assert.AreEqual(anchorOffset, selectionEnd, "Selection end should be the anchor offset");
    }

    private static TextView GetTextViewInstance(TextEditor editor)
        => editor.TextArea.TextView;
}
