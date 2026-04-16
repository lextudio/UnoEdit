using System.Xml;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Utils;
using NUnit.Framework;

namespace UnoEdit.Tests.Highlighting;

[TestFixture]
public class HighlightingBehaviorTests
{
    [Test]
    public void HighlightingColorizer_CreatesDocumentHighlighterService()
    {
        var textView = new ICSharpCode.AvalonEdit.Rendering.TextView
        {
            Document = new TextDocument("public class Demo {}\n")
        };
        var colorizer = new HighlightingColorizer(HighlightingManager.Instance.GetDefinition("C#"));

        ((ICSharpCode.AvalonEdit.Rendering.ITextViewConnect)colorizer).AddToTextView(textView);
        try
        {
            Assert.That(textView.Services.GetService(typeof(IHighlighter)), Is.Not.Null);
        }
        finally
        {
            ((ICSharpCode.AvalonEdit.Rendering.ITextViewConnect)colorizer).RemoveFromTextView(textView);
        }
    }

    [Test]
    public void DocumentPrinter_CreatesPortablePrintedDescriptor()
    {
        var document = new TextDocument("alpha\nbeta");

        var result = (DocumentPrinter.PrintedDocument)DocumentPrinter.ConvertTextDocumentToBlock(document, null);

        Assert.That(result.Lines.Count, Is.EqualTo(2));
        Assert.That(result.Lines[0].Text, Is.EqualTo("alpha"));
    }

    [Test]
    public void DefaultIndentationStrategy_IndentLines_ReindentsWholeRange()
    {
        var document = new TextDocument("    first\nsecond\nthird");
        var strategy = new DefaultIndentationStrategy();

        strategy.IndentLines(document, 2, 3);

        Assert.That(document.GetLineByNumber(2).Length, Is.EqualTo("    second".Length));
        Assert.That(document.GetLineByNumber(3).Length, Is.EqualTo("    third".Length));
        Assert.That(document.GetText(document.GetLineByNumber(2)), Is.EqualTo("    second"));
        Assert.That(document.GetText(document.GetLineByNumber(3)), Is.EqualTo("    third"));
    }

    [Test]
    public void DocumentHighlighter_DefaultTextColor_IsNotNull()
    {
        using var highlighter = new DocumentHighlighter(new TextDocument("alpha"), HighlightingManager.Instance.GetDefinition("C#"));

        Assert.That(highlighter.DefaultTextColor, Is.Not.Null);
    }

    [Test]
    public void XshdProperty_AcceptVisitor_UsesVisitProperty()
    {
        var property = new XshdProperty { Name = "name", Value = "value" };
        var visitor = new TestVisitor();

        object result = property.AcceptVisitor(visitor);

        Assert.That(visitor.VisitPropertyCalls, Is.EqualTo(1));
        Assert.That(result, Is.SameAs(property));
    }

    [Test]
    public void SaveXshdVisitor_WritesPropertyElement()
    {
        var definition = new XshdSyntaxDefinition { Name = "Demo" };
        definition.Elements.Add(new XshdProperty { Name = "mimeType", Value = "text/demo" });

        var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
        using var stringWriter = new System.IO.StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        var visitor = new SaveXshdVisitor(xmlWriter);

        visitor.WriteDefinition(definition);
        xmlWriter.Flush();

        Assert.That(stringWriter.ToString(), Does.Contain("Property"));
        Assert.That(stringWriter.ToString(), Does.Contain("mimeType"));
    }

    sealed class TestVisitor : IXshdVisitor
    {
        public int VisitPropertyCalls { get; private set; }
        public object VisitRuleSet(XshdRuleSet ruleSet) => ruleSet;
        public object VisitColor(XshdColor color) => color;
        public object VisitKeywords(XshdKeywords keywords) => keywords;
        public object VisitSpan(XshdSpan span) => span;
        public object VisitImport(XshdImport import) => import;
        public object VisitRule(XshdRule rule) => rule;
        public object VisitProperty(XshdProperty property)
        {
            VisitPropertyCalls++;
            return property;
        }
    }
}
