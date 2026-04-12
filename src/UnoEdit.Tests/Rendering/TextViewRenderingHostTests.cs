using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextViewRenderingHostTests
{
    [Test]
    public void Redraw_InvalidatesVisualLines_AndRaisesHooks()
    {
        var textView = new RecordingTextView
        {
            VisualLinesValid = true
        };

        int visualLinesChangedCount = 0;
        textView.VisualLinesChanged += (_, _) => visualLinesChangedCount++;

        textView.Redraw();

        Assert.That(textView.VisualLinesValid, Is.False);
        Assert.That(visualLinesChangedCount, Is.EqualTo(1));
        Assert.That(textView.RedrawRequestedCount, Is.EqualTo(1));
    }

    [Test]
    public void InvalidateLayer_TracksLayerAndRaisesHook()
    {
        var textView = new RecordingTextView();

        textView.InvalidateLayer(KnownLayer.Selection);
        textView.InvalidateLayer(KnownLayer.Caret);

        Assert.That(textView.InvalidatedLayerNotifications, Is.EqualTo(new[] { KnownLayer.Selection, KnownLayer.Caret }));
        Assert.That(textView.CapturedInvalidatedLayers, Does.Contain(KnownLayer.Selection));
        Assert.That(textView.CapturedInvalidatedLayers, Does.Contain(KnownLayer.Caret));
    }

    sealed class RecordingTextView : TextView
    {
        public int RedrawRequestedCount { get; private set; }

        public List<KnownLayer> InvalidatedLayerNotifications { get; } = new();

        public IReadOnlyCollection<KnownLayer> CapturedInvalidatedLayers => InvalidatedLayers;

        protected override void OnRedrawRequested(EventArgs e)
        {
            RedrawRequestedCount++;
        }

        protected override void OnLayerInvalidated(KnownLayer knownLayer)
        {
            InvalidatedLayerNotifications.Add(knownLayer);
        }
    }
}
