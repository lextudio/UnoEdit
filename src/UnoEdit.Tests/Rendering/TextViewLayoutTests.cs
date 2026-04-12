using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;
using Windows.Foundation;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextViewLayoutTests
{
    [Test]
    public void EnsureVisualLines_BuildsSimpleVisualLineModel()
    {
        var textView = new TextView
        {
            Document = new TextDocument("one\ntwo\nthree"),
            DefaultLineHeight = 20
        };

        textView.EnsureVisualLines();

        Assert.That(textView.VisualLinesValid, Is.True);
        Assert.That(textView.VisualLines.Count, Is.EqualTo(3));
        Assert.That(textView.DocumentHeight, Is.EqualTo(60));
        Assert.That(textView.VisualLines.Select(v => v.FirstDocumentLine.LineNumber), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void VisualTopMapping_UsesConstructedVisualLines()
    {
        var textView = new TextView
        {
            Document = new TextDocument("one\ntwo\nthree"),
            DefaultLineHeight = 20
        };

        Assert.That(textView.GetVisualTopByDocumentLine(1), Is.EqualTo(0));
        Assert.That(textView.GetVisualTopByDocumentLine(3), Is.EqualTo(40));

        Assert.That(textView.GetDocumentLineByVisualTop(0)?.LineNumber, Is.EqualTo(1));
        Assert.That(textView.GetDocumentLineByVisualTop(21)?.LineNumber, Is.EqualTo(2));
        Assert.That(textView.GetDocumentLineByVisualTop(45)?.LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void PositionMapping_FallsBackToConstructedVisualLines()
    {
        var textView = new TextView
        {
            Document = new TextDocument("one\ntwo"),
            DefaultLineHeight = 20
        };

        TextViewPosition? position = textView.GetPositionFloor(new Point(5, 25));
        Point visual = textView.GetVisualPosition(new TextViewPosition(2, 2), VisualYPosition.LineTop);

        Assert.That(position.HasValue, Is.True);
        Assert.That(position?.Line, Is.EqualTo(2));
        Assert.That(position?.Column, Is.EqualTo(1));
        Assert.That(visual.Y, Is.EqualTo(20));
    }
}
