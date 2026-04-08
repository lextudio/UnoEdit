using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
    [TestFixture]
    public class TabColumnHelperTests
    {
        // ── LogicalToVisualColumn ────────────────────────────────────────────────────

        [Test]
        public void NoTabs_VisualEqualsLogical()
        {
            const string text = "abcdef";
            for (int i = 0; i <= text.Length; i++)
                Assert.That(TabColumnHelper.LogicalToVisualColumn(text, i), Is.EqualTo(i));
        }

        [Test]
        public void TabAtStart_SnapsToFirstStop()
        {
            // "\t" at col 0 → next stop = 4
            Assert.That(TabColumnHelper.LogicalToVisualColumn("\tabc", 1), Is.EqualTo(4));
        }

        [Test]
        public void TabAtColumn2_SnapsToNextStop()
        {
            // "ab\tc": 'c' is at logical index 3; tab from visual 2 advances to col 4, so 'c' starts at visual col 4
            Assert.That(TabColumnHelper.LogicalToVisualColumn("ab\tc", 3), Is.EqualTo(4));
        }

        [Test]
        public void TwoConsecutiveTabs_AdvanceTwoStops()
        {
            // "\t\t" → first tab: 0→4, second tab: 4→8
            Assert.That(TabColumnHelper.LogicalToVisualColumn("\t\t", 2), Is.EqualTo(8));
        }

        [Test]
        public void LogicalBeyondLength_ReturnsMaxVisual()
        {
            const string text = "abc";
            int maxVis = TabColumnHelper.LogicalToVisualColumn(text, text.Length);
            Assert.That(TabColumnHelper.LogicalToVisualColumn(text, text.Length + 5), Is.EqualTo(maxVis));
        }

        // ── VisualToLogicalColumn ────────────────────────────────────────────────────

        [Test]
        public void NoTabs_LogicalEqualsVisual()
        {
            const string text = "abcdef";
            for (int i = 0; i <= text.Length; i++)
                Assert.That(TabColumnHelper.VisualToLogicalColumn(text, i), Is.EqualTo(i));
        }

        [Test]
        public void VisualInsideTabSpan_SnapsCorrectly()
        {
            // "\tabc": tab occupies visual columns [0,4). Visual col 2 is closer to col 0 start (2 vs 2 → tie → snap to start char i=0).
            // The midpoint is (0+4)/2 = 2, so col 2 snaps to logical 0.
            Assert.That(TabColumnHelper.VisualToLogicalColumn("\tabc", 2), Is.EqualTo(0));
            // Visual col 3 > midpoint(2), snaps to logical 1 (the 'a' after the tab)
            Assert.That(TabColumnHelper.VisualToLogicalColumn("\tabc", 3), Is.EqualTo(1));
        }

        [Test]
        public void VisualBeyondText_ReturnsTextLength()
        {
            const string text = "abc";
            Assert.That(TabColumnHelper.VisualToLogicalColumn(text, 100), Is.EqualTo(text.Length));
        }

        // ── Round-trip ────────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_PlainText()
        {
            const string text = "hello world";
            for (int i = 0; i <= text.Length; i++)
            {
                int vis = TabColumnHelper.LogicalToVisualColumn(text, i);
                int back = TabColumnHelper.VisualToLogicalColumn(text, vis);
                Assert.That(back, Is.EqualTo(i), $"Round-trip failed at logical column {i}");
            }
        }

        [Test]
        public void RoundTrip_WithTabs()
        {
            const string text = "\tif\t(x)\t{}";
            for (int i = 0; i <= text.Length; i++)
            {
                int vis = TabColumnHelper.LogicalToVisualColumn(text, i);
                int back = TabColumnHelper.VisualToLogicalColumn(text, vis);
                Assert.That(back, Is.EqualTo(i), $"Round-trip failed at logical column {i}");
            }
        }
    }
}
