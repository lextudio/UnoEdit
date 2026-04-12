using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.Foundation;

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
            PreparedText = (FormattedTextElement.PreparedTextDescriptor)FormattedTextElement.PrepareText("formatter", "abc", properties)
        };
        element.SetTextRunPropertiesForTests(properties);
        var run = new FormattedTextRun(element, properties);

        var formatted = (VisualLineElement.TextRunDescriptor)run.Format(120);
        Rect bounds = run.ComputeBoundingBox(false, false);

        Assert.That(run.CharacterBufferReference, Is.EqualTo("abc"));
        Assert.That(formatted.Text, Is.EqualTo("abc"));
        Assert.That(bounds.Width, Is.EqualTo(3));
        Assert.That(bounds.Height, Is.EqualTo(1));
    }

    [Test]
    public void DrawMethods_RecordIntoDrawingContext()
    {
        var properties = new VisualLineElementTextRunProperties();
        var element = new FormattedTextElement(1)
        {
            PreparedText = (FormattedTextElement.PreparedTextDescriptor)FormattedTextElement.PrepareText("formatter", "x", properties)
        };
        element.SetTextRunPropertiesForTests(properties);
        var formattedRun = new FormattedTextRun(element, properties);
        var inlineRun = new InlineObjectRun(1, properties, null!);
        var drawingContext = new System.Windows.Media.DrawingContext();

        formattedRun.Draw(drawingContext, new Point(1, 2), false, false);
        inlineRun.Draw(drawingContext, new Point(3, 4), true, false);

        Assert.That(drawingContext.Operations.Count, Is.EqualTo(2));
        Assert.That(((System.Windows.Media.DrawingContext.DrawOperation)drawingContext.Operations[0]).Kind, Is.EqualTo("formatted-text"));
        Assert.That(((System.Windows.Media.DrawingContext.DrawOperation)drawingContext.Operations[1]).Kind, Is.EqualTo("inline-object"));
    }
}
