using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextViewRenderingHostTests
{
    [Test]
    public void Redraw_InvalidatesVisualLines_AndRaisesHooks()
    {
        var textView = new TextView
        {
            VisualLinesValid = true
        };

        int visualLinesChangedCount = 0;
        textView.VisualLinesChanged += (_, _) => visualLinesChangedCount++;

        textView.Redraw();

        Assert.That(textView.VisualLinesValid, Is.False);
        Assert.That(visualLinesChangedCount, Is.EqualTo(1));
    }

    [Test]
    public void InvalidateLayer_TracksLayer()
    {
        var textView = new TextView();

        textView.InvalidateLayer(KnownLayer.Selection);
        textView.InvalidateLayer(KnownLayer.Caret);

        Assert.That(textView.InvalidatedLayers, Does.Contain(KnownLayer.Selection));
        Assert.That(textView.InvalidatedLayers, Does.Contain(KnownLayer.Caret));
    }
}
