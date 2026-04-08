// EditorRuntimeTests — UI thread tests that mount the TextEditor control live.
// Triggered headlessly when UNO_RUNTIME_TESTS_RUN_TESTS env var is set.

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class EditorRuntimeTests
{
    // -----------------------------------------------------------------------
    // TextEditor — document binding and line count
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task When_DocumentSet_LineCountMatchesText()
    {
        const string text = "line1\nline2\nline3\nline4\nline5";
        var doc = new TextDocument(text);

        var editor = new TextEditor();
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.Document = doc;
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(5, doc.LineCount,
            "Document should report 5 lines for the test content.");
    }

    [TestMethod]
    public async Task When_OffsetSet_CurrentOffsetUpdates()
    {
        var doc = new TextDocument("hello world");
        var editor = new TextEditor { Document = doc };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.CurrentOffset = 5;
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(5, editor.CurrentOffset);
    }

    [TestMethod]
    public async Task When_SelectionSet_StartAndEndOffsetsUpdate()
    {
        var doc = new TextDocument("hello world");
        var editor = new TextEditor { Document = doc };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.SetSelection(2, 7);
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(2, editor.SelectionStartOffset, "SelectionStartOffset should be 2.");
        Assert.AreEqual(7, editor.SelectionEndOffset, "SelectionEndOffset should be 7.");
    }

    [TestMethod]
    public async Task When_DocumentReplaced_NewDocumentReflectedInEditor()
    {
        var doc1 = new TextDocument("first");
        var doc2 = new TextDocument("second document\nwith two lines");

        var editor = new TextEditor { Document = doc1 };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.Document = doc2;
        await UnitTestsUIContentHelper.WaitForIdle();

        Assert.AreEqual(doc2, editor.Document);
        Assert.AreEqual(2, doc2.LineCount,
            "Replaced document should have 2 lines.");
    }

    // -----------------------------------------------------------------------
    // FoldingManager integration via TextEditor
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task When_FoldingManagerAttached_FoldsAreReflected()
    {
        var doc = new TextDocument("A\nB\nC\nD\nE");
        var fm = new FoldingManager(doc);

        int l1End = doc.GetLineByNumber(1).EndOffset;
        int l3End = doc.GetLineByNumber(3).EndOffset;
        var fold = fm.CreateFolding(l1End, l3End);
        fold.IsFolded = true;

        var editor = new TextEditor { Document = doc, FoldingManager = fm };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        // Verify the fold reports the expected offsets
        Assert.AreEqual(l1End, fold.StartOffset);
        Assert.AreEqual(l3End, fold.EndOffset);
        Assert.IsTrue(fold.IsFolded, "Fold should remain folded after editor mount.");
    }
}
