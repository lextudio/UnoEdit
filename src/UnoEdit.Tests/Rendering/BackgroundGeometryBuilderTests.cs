using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class BackgroundGeometryBuilderTests
{
    [Test]
    public void CloseFigure_RecordsBoundaryAfterRectangles()
    {
        var builder = new BackgroundGeometryBuilder();

        builder.AddRectangle(1, 2, 5, 8);
        builder.CloseFigure();
        builder.CloseFigure();
        builder.AddRectangle(10, 20, 15, 25);
        builder.CloseFigure();

        Assert.That(builder.ClosedFigureOffsets, Is.EqualTo(new[] { 1, 2 }));
    }
}
