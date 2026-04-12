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
            DefaultLineHeight = 20,
            WideSpaceWidth = 4
        };

        TextViewPosition? position = textView.GetPositionFloor(new Point(5, 25));
        Point visual = textView.GetVisualPosition(new TextViewPosition(2, 2), VisualYPosition.LineTop);

        Assert.That(position.HasValue, Is.True);
        Assert.That(position?.Line, Is.EqualTo(2));
        Assert.That(position?.Column, Is.EqualTo(1));
        Assert.That(visual.Y, Is.EqualTo(20));
        Assert.That(visual.X, Is.EqualTo(4));
    }

    [Test]
    public void VisualColumnHelpers_UseSingleLineCharacterWidthModel()
    {
        var textView = new TextView
        {
            Document = new TextDocument("hello"),
            DefaultLineHeight = 20,
            WideSpaceWidth = 5
        };

        textView.EnsureVisualLines();
        VisualLine visualLine = textView.VisualLines[0];
        UnoTextLine textLine = visualLine.TextLines[0];

        Assert.That(visualLine.GetTextLineVisualStartColumn(textLine), Is.EqualTo(0));
        Assert.That(visualLine.GetTextLineVisualXPosition(textLine, 3), Is.EqualTo(15));
        Assert.That(visualLine.GetVisualColumnFloor(new Point(14.9, 0)), Is.EqualTo(2));
        Assert.That(visualLine.GetVisualColumnFloor(new Point(40, 0)), Is.EqualTo(5));
        Assert.That(visualLine.GetVisualColumnFloor(new Point(40, 0), allowVirtualSpace: true), Is.EqualTo(8));
    }

    [Test]
    public void CollapseLines_UsesSharedHeightTreeBackend()
    {
        var textView = new TextView
        {
            Document = new TextDocument("one\ntwo\nthree"),
            DefaultLineHeight = 20
        };

        var start = textView.Document.GetLineByNumber(1);
        var end = textView.Document.GetLineByNumber(2);

        CollapsedLineSection section = textView.CollapseLines(start, end);

        Assert.That(section, Is.Not.Null);
        Assert.That(section.IsCollapsed, Is.True);
        Assert.That(section.Start, Is.SameAs(start));
        Assert.That(section.End, Is.SameAs(end));

        section.Uncollapse();

        Assert.That(section.IsCollapsed, Is.False);
        Assert.That(section.Start, Is.Null);
        Assert.That(section.End, Is.Null);
    }
}
