using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;
using Microsoft.UI.Xaml;
using System.ComponentModel.Design;
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

    [Test]
    public void FormattedTextElement_StringConstructor_PreparesTextOnDemand()
    {
        var document = new TextDocument("abc");
        var textView = new TextView { Document = document };
        var visualLine = new VisualLine(textView, document.GetLineByNumber(1));
        var context = new TestTextRunConstructionContext(document, textView, visualLine);
        var element = new FormattedTextElement("abc", 3);
        element.SetTextRunPropertiesForTests(new VisualLineElementTextRunProperties());

        var run = new FormattedTextRun(element, element.TextRunProperties);
        var metricsBefore = run.Format(100);
        element.CreateTextRun(0, context);
        var metricsAfter = run.Format(100);

        Assert.That(element.PreparedText, Is.Not.Null);
        Assert.That(metricsAfter.Width, Is.GreaterThan(metricsBefore.Width));
    }

    [Test]
    public void FormattedTextElement_FormattedTextConstructor_UsesFormattedMetrics()
    {
        var formattedText = new System.Windows.Media.FormattedText(
            "abcd",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new System.Windows.Media.Typeface("Consolas"),
            10,
            null);

        var element = new FormattedTextElement(formattedText, 4);
        element.SetTextRunPropertiesForTests(new VisualLineElementTextRunProperties());
        var run = new FormattedTextRun(element, element.TextRunProperties);

        var metrics = run.Format(100);

        Assert.That(metrics.Width, Is.EqualTo(formattedText.WidthIncludingTrailingWhitespace).Within(0.001));
        Assert.That(metrics.Height, Is.EqualTo(formattedText.Height).Within(0.001));
    }

    [Test]
    public void AddInlineObject_UsesHostMeasurementForInlineMetrics()
    {
        var document = new TextDocument("x");
        var host = new FakeInlineObjectHost(new Size(42, 7));
        var container = document.ServiceProvider as ServiceContainer;
        container!.AddService(typeof(IInlineObjectHost), host);

        var textView = new TextView { Document = document };
        var properties = new VisualLineElementTextRunProperties();
        var inlineRun = new InlineObjectRun(1, properties, null!);

        textView.AddInlineObject(inlineRun);
        var metrics = inlineRun.Format(100);
        var bounds = inlineRun.ComputeBoundingBox(false, false);

        Assert.That(host.AttachCalls, Is.EqualTo(1));
        Assert.That(host.MeasureCalls, Is.EqualTo(1));
        Assert.That(metrics.Width, Is.EqualTo(42));
        Assert.That(metrics.Height, Is.EqualTo(7));
        Assert.That(metrics.Baseline, Is.EqualTo(5));
        Assert.That(bounds.X, Is.EqualTo(0));
        Assert.That(bounds.Y, Is.EqualTo(-5));
        Assert.That(bounds.Width, Is.EqualTo(42));
        Assert.That(bounds.Height, Is.EqualTo(7));
    }

    sealed class TestTextRunConstructionContext : ITextRunConstructionContext
    {
        public TestTextRunConstructionContext(TextDocument document, TextView textView, VisualLine visualLine)
        {
            Document = document;
            TextView = textView;
            VisualLine = visualLine;
        }

        public TextDocument Document { get; }
        public TextView TextView { get; }
        public VisualLine VisualLine { get; }
        public TextRunProperties GlobalTextRunProperties { get; } = new VisualLineElementTextRunProperties();

        public StringSegment GetText(int offset, int length)
        {
            return new StringSegment(Document.GetText(offset, length));
        }
    }

    sealed class FakeInlineObjectHost : IInlineObjectHost
    {
        readonly Size measuredSize;
        readonly double baseline;

        public FakeInlineObjectHost(Size measuredSize, double baseline = 5)
        {
            this.measuredSize = measuredSize;
            this.baseline = baseline;
        }

        public int AttachCalls { get; private set; }
        public int DetachCalls { get; private set; }
        public int MeasureCalls { get; private set; }

        public void AttachInlineElement(UIElement element)
        {
            AttachCalls++;
        }

        public void DetachInlineElement(UIElement element)
        {
            DetachCalls++;
        }

        public InlineElementMetrics MeasureInlineElement(UIElement element)
        {
            MeasureCalls++;
            return new InlineElementMetrics(measuredSize, baseline);
        }
    }
}
