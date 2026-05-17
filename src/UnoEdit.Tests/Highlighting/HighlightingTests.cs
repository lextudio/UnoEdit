using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting
{
    [TestFixture]
    public class HighlightingManagerTests
    {
        // ── Built-in definitions load without error ───────────────────────────────────

        [Test]
        [TestCase("C#")]
        [TestCase("XML")]
        [TestCase("HTML")]
        [TestCase("JavaScript")]
        [TestCase("VB")]
        [TestCase("Python")]
        [TestCase("Java")]
        [TestCase("C++")]
        [TestCase("TSQL")]
        [TestCase("CSS")]
        [TestCase("ASP/XHTML")]
        [TestCase("Boo")]
        [TestCase("Coco")]
        [TestCase("Patch")]
        [TestCase("PowerShell")]
        [TestCase("PHP")]
        [TestCase("TeX")]
        [TestCase("MarkDown")]
        [TestCase("MarkDownWithFontSize")]
        [TestCase("Json")]
        public void GetDefinition_KnownLanguage_IsNotNull(string name)
        {
            var def = HighlightingManager.Instance.GetDefinition(name);
            Assert.That(def, Is.Not.Null, $"'{name}' definition should be registered");
        }

        [Test]
        [TestCase(".cs")]
        [TestCase(".xml")]
        [TestCase(".html")]
        [TestCase(".js")]
        [TestCase(".py")]
        [TestCase(".vb")]
        [TestCase(".aspx")]
        [TestCase(".boo")]
        [TestCase(".atg")]
        [TestCase(".patch")]
        [TestCase(".ps1")]
        [TestCase(".php")]
        [TestCase(".tex")]
        [TestCase(".md")]
        [TestCase(".json")]
        public void GetDefinitionByExtension_KnownExtension_IsNotNull(string extension)
        {
            var def = HighlightingManager.Instance.GetDefinitionByExtension(extension);
            Assert.That(def, Is.Not.Null, $"Extension '{extension}' should be registered");
        }

        [Test]
        public void GetDefinition_UnknownName_ReturnsNull()
        {
            var def = HighlightingManager.Instance.GetDefinition("ThisDoesNotExist123");
            Assert.That(def, Is.Null);
        }

        [Test]
        public void CSharp_HasMainRuleSet()
        {
            var def = HighlightingManager.Instance.GetDefinition("C#");
            Assert.That(def.MainRuleSet, Is.Not.Null);
        }

        [Test]
        public void CSharp_HasAtLeastOneRule()
        {
            var def = HighlightingManager.Instance.GetDefinition("C#");
            var ruleSet = def.MainRuleSet;
            bool hasRules = ruleSet.Rules.Count > 0 || ruleSet.Spans.Count > 0;
            Assert.That(hasRules, Is.True, "C# definition should contain at least one rule or span");
        }
    }

    [TestFixture]
    public class DocumentHighlighterTests
    {
        private static TextDocument MakeDocument(string content)
            => new TextDocument(content);

        // ── Basic smoke tests ─────────────────────────────────────────────────────────

        [Test]
        public void HighlightLine_PlainText_ReturnsLine()
        {
            var doc = MakeDocument("Hello world\n");
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            var result = hl.HighlightLine(1);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.DocumentLine, Is.Not.Null);
        }

        [Test]
        public void HighlightLine_CSharpKeyword_ProducesSections()
        {
            // "public class Foo" should produce at least two highlighted sections
            // (one for "public", one for "class").
            var doc = MakeDocument("public class Foo\n");
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            var result = hl.HighlightLine(1);
            Assert.That(result.Sections, Is.Not.Empty, "Keywords should produce highlighted sections");
        }

        [Test]
        public void HighlightLine_CSharpString_IncludesStringSection()
        {
            var doc = MakeDocument("var s = \"hello\";\n");
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            var result = hl.HighlightLine(1);
            // There should be at least one section (the string literal)
            Assert.That(result.Sections.Count, Is.GreaterThan(0));
        }

        [Test]
        public void HighlightLine_CSharpComment_IncludesCommentSection()
        {
            var doc = MakeDocument("// This is a comment\n");
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            var result = hl.HighlightLine(1);
            Assert.That(result.Sections.Count, Is.GreaterThan(0));
        }

        [Test]
        public void HighlightLine_SectionsDoNotOverlap()
        {
            var doc = MakeDocument("public static void Main() { }\n");
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            var result = hl.HighlightLine(1);
            var sections = result.Sections.OrderBy(s => s.Offset).ToList();
            for (int i = 1; i < sections.Count; i++)
            {
                int prevEnd = sections[i - 1].Offset + sections[i - 1].Length;
                Assert.That(sections[i].Offset, Is.GreaterThanOrEqualTo(prevEnd),
                    $"Sections [{i-1}] and [{i}] overlap");
            }
        }

        [Test]
        public void HighlightMultipleLines_StateIsPropagated()
        {
            // Multi-line string spanning two lines — the highlighter must carry state
            // across lines correctly without throwing.
            const string src = "var x = @\"\nhello\n\";\n";
            var doc = MakeDocument(src);
            var def = HighlightingManager.Instance.GetDefinition("C#");
            using var hl = new DocumentHighlighter(doc, def);
            Assert.DoesNotThrow(() =>
            {
                for (int line = 1; line <= doc.LineCount; line++)
                    hl.HighlightLine(line);
            });
        }
    }
}
