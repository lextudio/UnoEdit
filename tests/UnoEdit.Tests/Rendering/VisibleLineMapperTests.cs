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
    }
}
