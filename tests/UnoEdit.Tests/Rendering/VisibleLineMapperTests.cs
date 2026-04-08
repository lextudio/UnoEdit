using NUnit.Framework;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;

namespace UnoEdit.Tests.Rendering
{
    [TestFixture]
    public class VisibleLineMapperTests
    {
        private static TextDocument MakeDocument(string text) => new TextDocument(text);

        [Test]
        public void HidesLinesInsideFoldWhenFolded()
        {
            var doc = MakeDocument("l1\nl2\nl3\nl4\nl5");
            var fm = new FoldingManager(doc);
            int start = doc.GetLineByNumber(2).Offset;
            int end   = doc.GetLineByNumber(4).EndOffset;
            fm.CreateFolding(start, end).IsFolded = true;

            using var mapper = new VisibleLineMapper(doc, fm);
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 2, 5 }));
            Assert.That(mapper.GetVisualRow(1), Is.EqualTo(0));
            Assert.That(mapper.GetVisualRow(2), Is.EqualTo(1));
            Assert.That(mapper.GetVisualRow(3), Is.EqualTo(-1));
            Assert.That(mapper.GetVisualRow(4), Is.EqualTo(-1));
            Assert.That(mapper.GetVisualRow(5), Is.EqualTo(2));
        }

        [Test]
        public void RebuildsWhenFoldStateChanges()
        {
            var doc = MakeDocument("a\nb\nc");
            var fm = new FoldingManager(doc);
            var section = fm.CreateFolding(doc.GetLineByNumber(1).Offset, doc.GetLineByNumber(2).EndOffset);
            using var mapper = new VisibleLineMapper(doc, fm);

            // Initially not folded
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 2, 3 }));

            section.IsFolded = true;
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 3 }));

            section.IsFolded = false;
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public void NestedFolds_HiddenLinesCorrect()
        {
            var doc = MakeDocument("L1\nL2\nL3\nL4\nL5");
            var fm = new FoldingManager(doc);
            // Outer fold L2..L5
            var outer = fm.CreateFolding(doc.GetLineByNumber(2).Offset, doc.GetLineByNumber(5).EndOffset);
            // Inner fold L3..L4
            var inner = fm.CreateFolding(doc.GetLineByNumber(3).Offset, doc.GetLineByNumber(4).EndOffset);

            outer.IsFolded = true;
            inner.IsFolded = true; // inner folded while outer folded

            using var mapper = new VisibleLineMapper(doc, fm);
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 2 }));
            Assert.That(mapper.GetVisualRow(3), Is.EqualTo(-1));
        }

        [Test]
        public void OverlappingFolds_BehavePredictably()
        {
            var doc = MakeDocument("A\nB\nC\nD\nE");
            var fm = new FoldingManager(doc);
            fm.CreateFolding(doc.GetLineByNumber(1).Offset, doc.GetLineByNumber(4).EndOffset); // 1..4
            fm.CreateFolding(doc.GetLineByNumber(3).Offset, doc.GetLineByNumber(5).EndOffset); // 3..5

            // Fold the second section only
            var folds = fm.GetFoldingsAt(doc.GetLineByNumber(3).Offset);
            foreach (var f in folds) f.IsFolded = true;

            using var mapper = new VisibleLineMapper(doc, fm);
            // Start lines remain visible; hidden lines should be >start and <=end.
            Assert.That(mapper.VisibleLines, Is.EqualTo(new List<int> { 1, 2, 3 }));
            Assert.That(mapper.IsLineHidden(4), Is.True);
        }
    }
}
