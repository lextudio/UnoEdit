using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using NUnit.Framework;

namespace UnoEdit.Tests.Folding
{
    [TestFixture]
    public class FoldingManagerTests
    {
        private static TextDocument MakeDocument(string text)
            => new TextDocument(text);

        // ------------------------------------------------------------------
        // FoldingManager — create / remove
        // ------------------------------------------------------------------

        [Test]
        public void CreateFolding_StoresSection()
        {
            var doc = MakeDocument("line1\nline2\nline3\n");
            var fm = new FoldingManager(doc);

            var section = fm.CreateFolding(0, 11); // "line1\nline2"
            Assert.That(fm.AllFoldings.Count(), Is.EqualTo(1));
            Assert.That(section.StartOffset, Is.EqualTo(0));
            Assert.That(section.EndOffset, Is.EqualTo(11));
        }

        [Test]
        public void RemoveFolding_RemovesSection()
        {
            var doc = MakeDocument("line1\nline2\nline3\n");
            var fm = new FoldingManager(doc);

            var section = fm.CreateFolding(0, 11);
            fm.RemoveFolding(section);
            Assert.That(fm.AllFoldings.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Clear_RemovesAllSections()
        {
            var doc = MakeDocument("aaa\nbbb\nccc\n");
            var fm = new FoldingManager(doc);

            fm.CreateFolding(0, 3);
            fm.CreateFolding(4, 7);
            fm.Clear();
            Assert.That(fm.AllFoldings.Count(), Is.EqualTo(0));
        }

        // ------------------------------------------------------------------
        // IsFolded state and FoldingsChanged event
        // ------------------------------------------------------------------

        [Test]
        public void IsFolded_DefaultIsFalse()
        {
            var doc = MakeDocument("line1\nline2\n");
            var fm = new FoldingManager(doc);
            var section = fm.CreateFolding(0, 11);
            Assert.That(section.IsFolded, Is.False);
        }

        [Test]
        public void IsFolded_RaisesFoldingsChanged()
        {
            var doc = MakeDocument("line1\nline2\n");
            var fm = new FoldingManager(doc);
            var section = fm.CreateFolding(0, 11);

            int eventCount = 0;
            fm.FoldingsChanged += (_, _) => eventCount++;

            section.IsFolded = true;
            Assert.That(eventCount, Is.EqualTo(1));

            section.IsFolded = false;
            Assert.That(eventCount, Is.EqualTo(2));
        }

        // ------------------------------------------------------------------
        // UpdateFoldings
        // ------------------------------------------------------------------

        [Test]
        public void UpdateFoldings_ReplacesExistingSections()
        {
            var doc = MakeDocument("line1\nline2\nline3\n");
            var fm = new FoldingManager(doc);
            fm.CreateFolding(0, 5);

            var newFoldings = new List<NewFolding>
            {
                new NewFolding(0, 11) { Name = "fold1" },
                new NewFolding(12, 17) { Name = "fold2" },
            };
            fm.UpdateFoldings(newFoldings, -1);

            var all = fm.AllFoldings.ToList();
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(all[0].Title, Is.EqualTo("fold1"));
            Assert.That(all[1].Title, Is.EqualTo("fold2"));
        }

        [Test]
        public void UpdateFoldings_PreservesFoldedState()
        {
            var doc = MakeDocument("line1\nline2\nline3\n");
            var fm = new FoldingManager(doc);

            var initial = new List<NewFolding>
            {
                new NewFolding(0, 11) { Name = "block" },
            };
            fm.UpdateFoldings(initial, -1);
            fm.AllFoldings.First().IsFolded = true;

            // Update with the same folding range — folded state should be preserved.
            fm.UpdateFoldings(initial, -1);
            Assert.That(fm.AllFoldings.First().IsFolded, Is.True);
        }

        // ------------------------------------------------------------------
        // Offset auto-update after document edits
        // ------------------------------------------------------------------

        [Test]
        public void Offsets_AreUpdatedAfterDocumentInsert()
        {
            var doc = MakeDocument("line1\nline2\n");
            var fm = new FoldingManager(doc);
            var section = fm.CreateFolding(6, 11); // "line2"

            // Insert text before the fold.
            doc.Insert(0, "XXX\n");
            Assert.That(section.StartOffset, Is.EqualTo(10)); // shifted by 4
            Assert.That(section.EndOffset, Is.EqualTo(15));
        }

        // ------------------------------------------------------------------
        // BraceFoldingStrategy
        // ------------------------------------------------------------------

        [Test]
        public void BraceFoldingStrategy_FindsNoFoldsInFlatCode()
        {
            var doc = MakeDocument("var x = 1;\nvar y = 2;\n");
            var strategy = new BraceFoldingStrategy();
            var foldings = strategy.CreateNewFoldings(doc).ToList();
            Assert.That(foldings.Count, Is.EqualTo(0));
        }

        [Test]
        public void BraceFoldingStrategy_FindsBraceBlock()
        {
            // A brace block that spans more than one line.
            var code = "class Foo\n{\n    int x;\n}\n";
            var doc = MakeDocument(code);
            var strategy = new BraceFoldingStrategy();
            var foldings = strategy.CreateNewFoldings(doc).ToList();
            Assert.That(foldings.Count, Is.EqualTo(1));
            Assert.That(code[foldings[0].StartOffset], Is.EqualTo('{'));
            // EndOffset is exclusive (one past the closing brace)
            Assert.That(code[foldings[0].EndOffset - 1], Is.EqualTo('}'));
        }

        [Test]
        public void BraceFoldingStrategy_SkipsSameLineBraces()
        {
            // Braces on the same line should NOT produce a fold.
            var code = "var x = new { A = 1 };\n";
            var doc = MakeDocument(code);
            var strategy = new BraceFoldingStrategy();
            var foldings = strategy.CreateNewFoldings(doc).ToList();
            Assert.That(foldings.Count, Is.EqualTo(0));
        }

        [Test]
        public void BraceFoldingStrategy_UpdateFoldings_PopulatesManager()
        {
            var code = "class Foo\n{\n    void Bar()\n    {\n    }\n}\n";
            var doc = MakeDocument(code);
            var fm = new FoldingManager(doc);
            var strategy = new BraceFoldingStrategy();
            strategy.UpdateFoldings(fm, doc);

            int count = fm.AllFoldings.Count();
            Assert.That(count, Is.EqualTo(2), "Expected two brace-fold sections (outer class + inner method).");
        }

        [Test]
        public void XmlFoldingStrategy_FindsElementAndCommentFolds()
        {
            var xml = "<root>\n  <!-- comment\n       line -->\n  <child>\n    <leaf>value</leaf>\n  </child>\n</root>\n";
            var doc = MakeDocument(xml);
            var strategy = new XmlFoldingStrategy();

            int firstErrorOffset;
            var foldings = strategy.CreateNewFoldings(doc, out firstErrorOffset).ToList();

            Assert.That(firstErrorOffset, Is.EqualTo(-1));
            Assert.That(foldings.Count, Is.EqualTo(3));
            Assert.That(foldings[0].Name, Is.EqualTo("<root>"));
            Assert.That(foldings[1].Name, Does.StartWith("<!-- comment"));
            Assert.That(foldings[2].Name, Is.EqualTo("<child>"));
        }

        [Test]
        public void XmlFoldingStrategy_ShowAttributesWhenFolded_UsesAttributeText()
        {
            var xml = "<root>\n  <child id=\"42\" name=\"demo\">\n    <leaf />\n  </child>\n</root>\n";
            var doc = MakeDocument(xml);
            var strategy = new XmlFoldingStrategy { ShowAttributesWhenFolded = true };

            int firstErrorOffset;
            var foldings = strategy.CreateNewFoldings(doc, out firstErrorOffset).ToList();

            Assert.That(firstErrorOffset, Is.EqualTo(-1));
            Assert.That(foldings.Select(static f => f.Name), Does.Contain("<child id=\"42\" name=\"demo\">"));
        }

        [Test]
        public void XmlFoldingStrategy_InvalidXml_ReturnsNoFoldingsAndFirstErrorOffset()
        {
            var xml = "<root>\n  <child>\n</root>\n";
            var doc = MakeDocument(xml);
            var strategy = new XmlFoldingStrategy();

            int firstErrorOffset;
            var foldings = strategy.CreateNewFoldings(doc, out firstErrorOffset).ToList();

            Assert.That(foldings, Is.Empty);
            Assert.That(firstErrorOffset, Is.GreaterThanOrEqualTo(0));
        }

        // ------------------------------------------------------------------
        // GetFoldingsContaining / GetFoldingsAt
        // ------------------------------------------------------------------

        [Test]
        public void GetFoldingsContaining_ReturnsWrappingSections()
        {
            var doc = MakeDocument("AAABBBCCC");
            var fm = new FoldingManager(doc);
            fm.CreateFolding(0, 9); // whole line
            fm.CreateFolding(3, 6); // middle

            var containing = fm.GetFoldingsContaining(4).ToList();
            // offset 4 is inside both sections
            Assert.That(containing.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetFoldingsAt_ReturnsExactStartOffsetMatch()
        {
            var doc = MakeDocument("AAABBBCCC");
            var fm = new FoldingManager(doc);
            fm.CreateFolding(0, 3);
            fm.CreateFolding(6, 9);

            // GetFoldingsAt returns foldings whose StartOffset equals the argument exactly.
            var at0 = fm.GetFoldingsAt(0).ToList();
            Assert.That(at0.Count, Is.EqualTo(1));
            Assert.That(at0[0].StartOffset, Is.EqualTo(0));

            // Offset 1 is inside fold [0,3] but not its start — no match.
            var at1 = fm.GetFoldingsAt(1).ToList();
            Assert.That(at1.Count, Is.EqualTo(0));

            // Use GetFoldingsContaining to find sections by containment.
            var containing1 = fm.GetFoldingsContaining(1).ToList();
            Assert.That(containing1.Count, Is.EqualTo(1));
            Assert.That(containing1[0].StartOffset, Is.EqualTo(0));
        }
    }
}
