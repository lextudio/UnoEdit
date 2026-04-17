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

}
