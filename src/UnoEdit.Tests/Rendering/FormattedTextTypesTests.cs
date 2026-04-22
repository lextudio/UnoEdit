using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.Foundation;
using System.Windows.Media.TextFormatting;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class FormattedTextTypesTests
{
    [Test]
    public void FormattedTextRun_UsesPreparedText_ForFormatAndBounds()
    {
        var properties = new VisualLineElementTextRunProperties();
        var element = new FormattedTextElement(3)
        {
            PreparedText = FormattedTextElement.PrepareText(TextFormatter.Create(), "abc", properties)
        };
        element.SetTextRunPropertiesForTests(properties);
        var run = new FormattedTextRun(element, properties);

        TextEmbeddedObjectMetrics formatted = run.Format(120);
        Rect bounds = run.ComputeBoundingBox(false, false);
        double preparedWidth = element.PreparedText.WidthIncludingTrailingWhitespace;
        double preparedHeight = element.PreparedText.Height;

        Assert.That(run.Length, Is.EqualTo(1));
        Assert.That(formatted.Width, Is.EqualTo(preparedWidth).Within(0.001));
        Assert.That(formatted.Height, Is.EqualTo(preparedHeight).Within(0.001));
        Assert.That(bounds.Width, Is.EqualTo(preparedWidth).Within(0.001));
        Assert.That(bounds.Height, Is.EqualTo(preparedHeight).Within(0.001));
    }

    [Test]
    public void DrawMethods_RecordIntoDrawingContext()
    {
        var properties = new VisualLineElementTextRunProperties();
        var element = new FormattedTextElement(1)
        {
            PreparedText = FormattedTextElement.PrepareText(TextFormatter.Create(), "x", properties)
        };
        element.SetTextRunPropertiesForTests(properties);
        var formattedRun = new FormattedTextRun(element, properties);
        var inlineRun = new InlineObjectRun(1, properties, null!);
        var drawingContext = new System.Windows.Media.DrawingContext();

        formattedRun.Draw(drawingContext, new Point(1, 2), false, false);
        inlineRun.Draw(drawingContext, new Point(3, 4), true, false);

        Assert.That(drawingContext.Operations.Count, Is.EqualTo(2));
        Assert.That(((System.Windows.Media.DrawingContext.DrawOperation)drawingContext.Operations[0]).Kind, Is.EqualTo("text-line"));
        Assert.That(((System.Windows.Media.DrawingContext.DrawOperation)drawingContext.Operations[1]).Kind, Is.EqualTo("inline-object"));
    }
}
