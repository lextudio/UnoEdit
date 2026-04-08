// FoldingRuntimeTests — Uno RuntimeTests (MSTest + RunsOnUIThread)
// These tests run headlessly when UNO_RUNTIME_TESTS_RUN_TESTS env var is set.
// They exercise FoldingManager and VisibleLineMapper using live UI-thread access.

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class FoldingRuntimeTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="TextDocument"/> whose text is the given lines joined by "\n".
    /// Must be called on the UI thread because TextDocument.VerifyAccess enforces it.
    /// </summary>
    private static TextDocument MakeDocument(params string[] lines)
        => new TextDocument(string.Join("\n", lines));

    // -----------------------------------------------------------------------
    // VisibleLineMapper — pure logic tests (no live control, but UI thread for document)
    // -----------------------------------------------------------------------

    [TestMethod]
    public void When_NoFolds_AllLinesVisible()
    {
        var doc = MakeDocument("line1", "line2", "line3");
        using var mapper = new VisibleLineMapper(doc, null);

        Assert.AreEqual(3, mapper.VisibleLines.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, mapper.VisibleLines.ToArray());
    }

    [TestMethod]
    public void When_FoldCreated_HiddenLinesExcludedFromMapper()
    {
        // 5-line document; fold lines 2-4 (offsets span lines 2–4)
        var doc = MakeDocument("A", "B", "C", "D", "E");
        var fm = new FoldingManager(doc);

        // lines 2–4: startOffset = end of line 1 + 1 = 2, endOffset = end of line 4
        int l1End = doc.GetLineByNumber(1).EndOffset; // after "A"
        int l4End = doc.GetLineByNumber(4).EndOffset; // after "D"
        var fold = fm.CreateFolding(l1End, l4End);
        fold.IsFolded = true;

        using var mapper = new VisibleLineMapper(doc, fm);

        // Lines 2, 3, 4 should be hidden; lines 1 and 5 visible
        Assert.IsFalse(mapper.IsLineHidden(1));
        Assert.IsTrue(mapper.IsLineHidden(2));
        Assert.IsTrue(mapper.IsLineHidden(3));
        Assert.IsTrue(mapper.IsLineHidden(4));
        Assert.IsFalse(mapper.IsLineHidden(5));

        CollectionAssert.AreEqual(
            new[] { 1, 5 },
            mapper.VisibleLines.ToArray(),
            "Only lines 1 and 5 should be in the visible list.");
    }

    [TestMethod]
    public void When_FoldToggled_MapperRebuildsAutomatically()
    {
        var doc = MakeDocument("A", "B", "C");
        var fm = new FoldingManager(doc);

        int l1End = doc.GetLineByNumber(1).EndOffset;
        int l2End = doc.GetLineByNumber(2).EndOffset;
        var fold = fm.CreateFolding(l1End, l2End);

        using var mapper = new VisibleLineMapper(doc, fm);

        // Initially unfolded — all 3 lines visible
        Assert.AreEqual(3, mapper.VisibleLines.Count);

        // Fold: line 2 becomes hidden
        fold.IsFolded = true;
        Assert.AreEqual(2, mapper.VisibleLines.Count,
            "After folding, the mapper should auto-rebuild via FoldingsChanged.");

        // Unfold: all lines visible again
        fold.IsFolded = false;
        Assert.AreEqual(3, mapper.VisibleLines.Count,
            "After unfolding, the mapper should auto-rebuild.");
    }

    [TestMethod]
    public void When_FoldRemoved_LineBecomesVisible()
    {
        var doc = MakeDocument("A", "B", "C");
        var fm = new FoldingManager(doc);

        int l1End = doc.GetLineByNumber(1).EndOffset;
        int l2End = doc.GetLineByNumber(2).EndOffset;
        var fold = fm.CreateFolding(l1End, l2End);
        fold.IsFolded = true;

        using var mapper = new VisibleLineMapper(doc, fm);
        Assert.AreEqual(2, mapper.VisibleLines.Count);

        fm.RemoveFolding(fold);
        Assert.AreEqual(3, mapper.VisibleLines.Count,
            "Removing a fold should make hidden lines visible again.");
    }

    [TestMethod]
    public void When_MultipleNestedFolds_InnerLinesRemainHidden()
    {
        // 6-line doc; outer fold covers lines 2–5, inner fold covers lines 3–4.
        var doc = MakeDocument("1", "2", "3", "4", "5", "6");
        var fm = new FoldingManager(doc);

        int l1End = doc.GetLineByNumber(1).EndOffset;
        int l5End = doc.GetLineByNumber(5).EndOffset;
        int l2End = doc.GetLineByNumber(2).EndOffset;
        int l4End = doc.GetLineByNumber(4).EndOffset;

        var outer = fm.CreateFolding(l1End, l5End);
        outer.IsFolded = true;
        var inner = fm.CreateFolding(l2End, l4End);
        inner.IsFolded = true;

        using var mapper = new VisibleLineMapper(doc, fm);

        // Lines 2–5 hidden by outer fold; inner fold also hides 3–4.
        // Visible: 1, 6
        CollectionAssert.AreEqual(new[] { 1, 6 }, mapper.VisibleLines.ToArray());
    }

    [TestMethod]
    public void GetVisualRow_ReturnsCorrectIndex()
    {
        var doc = MakeDocument("A", "B", "C", "D");
        var fm = new FoldingManager(doc);

        int l1End = doc.GetLineByNumber(1).EndOffset;
        int l3End = doc.GetLineByNumber(3).EndOffset;
        var fold = fm.CreateFolding(l1End, l3End);
        fold.IsFolded = true;

        using var mapper = new VisibleLineMapper(doc, fm);

        // Visible lines: 1, 4 => visual rows 0, 1
        Assert.AreEqual(0, mapper.GetVisualRow(1));
        Assert.AreEqual(-1, mapper.GetVisualRow(2), "Hidden line should return -1.");
        Assert.AreEqual(-1, mapper.GetVisualRow(3), "Hidden line should return -1.");
        Assert.AreEqual(1, mapper.GetVisualRow(4));
    }
}
