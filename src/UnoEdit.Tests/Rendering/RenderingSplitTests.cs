using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using NUnit.Framework;
using System.Windows.Media;

namespace UnoEdit.Tests.Rendering
{
    [TestFixture]
    public class RenderingSplitTests
    {
        [Test]
        public void TextView_Collections_ConnectAndDisconnectParticipants()
        {
            var textView = new TextView();
            var generator = new TrackingGenerator();
            var transformer = new TrackingTransformer();
            var renderer = new TrackingBackgroundRenderer();

            textView.ElementGenerators.Add(generator);
            textView.LineTransformers.Add(transformer);
            textView.BackgroundRenderers.Add(renderer);

            Assert.That(generator.AddedTo, Is.SameAs(textView));
            Assert.That(transformer.AddedTo, Is.SameAs(textView));
            Assert.That(renderer.AddedTo, Is.SameAs(textView));

            textView.ElementGenerators.Remove(generator);
            textView.LineTransformers.Remove(transformer);
            textView.BackgroundRenderers.Remove(renderer);

            Assert.That(generator.RemovedFrom, Is.SameAs(textView));
            Assert.That(transformer.RemovedFrom, Is.SameAs(textView));
            Assert.That(renderer.RemovedFrom, Is.SameAs(textView));
        }

        [Test]
        public void VisualLine_ConstructVisualElements_UsesTextAndGeneratorElements()
        {
            var document = new TextDocument("abcXYZ");
            var textView = new TextView { Document = document };
            var visualLine = new VisualLine(textView, document.GetLineByNumber(1));
            var context = new TestTextRunConstructionContext(document, textView, visualLine);
            var generator = new FixedInterestGenerator(3, 3);

            visualLine.ConstructVisualElements(context, new[] { generator });

            Assert.That(visualLine.Elements.Count, Is.EqualTo(2));
            Assert.That(visualLine.Elements[0], Is.TypeOf<VisualLineText>());
            Assert.That(visualLine.Elements[0].DocumentLength, Is.EqualTo(3));
            Assert.That(visualLine.Elements[1], Is.TypeOf<TestVisualLineElement>());
            Assert.That(visualLine.Elements[1].RelativeTextOffset, Is.EqualTo(3));
            Assert.That(visualLine.VisualLength, Is.EqualTo(6));
        }

        [Test]
        public void VisualLine_RunTransformers_CanModifyConstructedElements()
        {
            var document = new TextDocument("abcdef");
            var textView = new TextView { Document = document };
            var visualLine = new VisualLine(textView, document.GetLineByNumber(1));
            var context = new TestTextRunConstructionContext(document, textView, visualLine);

            visualLine.ConstructVisualElements(context, Array.Empty<VisualLineElementGenerator>());
            var transformer = new MarkingTransformer();

            visualLine.RunTransformers(context, new IVisualLineTransformer[] { transformer });

            Assert.That(transformer.SeenElements, Is.EqualTo(1));
            Assert.That(transformer.TransformCalls, Is.EqualTo(1));
            Assert.That(visualLine.Elements.Single().TextRunProperties, Is.Not.Null);
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

            public StringSegment GetText(int offset, int length)
            {
                return new StringSegment(Document.GetText(offset, length));
            }

            public object GlobalTextRunProperties => null;
        }

        sealed class TrackingGenerator : VisualLineElementGenerator, ITextViewConnect
        {
            public TextView AddedTo { get; private set; }
            public TextView RemovedFrom { get; private set; }

            public override int GetFirstInterestedOffset(int startOffset) => -1;
            public override VisualLineElement ConstructElement(int offset) => null;

            public void AddToTextView(TextView textView) => AddedTo = textView;
            public void RemoveFromTextView(TextView textView) => RemovedFrom = textView;
        }

        sealed class TrackingTransformer : IVisualLineTransformer, ITextViewConnect
        {
            public TextView AddedTo { get; private set; }
            public TextView RemovedFrom { get; private set; }

            public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
            {
            }

            public void AddToTextView(TextView textView) => AddedTo = textView;
            public void RemoveFromTextView(TextView textView) => RemovedFrom = textView;
        }

        sealed class TrackingBackgroundRenderer : IBackgroundRenderer, ITextViewConnect
        {
            public TextView AddedTo { get; private set; }
            public TextView RemovedFrom { get; private set; }

            public KnownLayer Layer => KnownLayer.Selection;

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
            }

            public void AddToTextView(TextView textView) => AddedTo = textView;
            public void RemoveFromTextView(TextView textView) => RemovedFrom = textView;
        }

        sealed class FixedInterestGenerator : VisualLineElementGenerator
        {
            readonly int offset;
            readonly int length;

            public FixedInterestGenerator(int offset, int length)
            {
                this.offset = offset;
                this.length = length;
            }

            public override int GetFirstInterestedOffset(int startOffset)
            {
                return startOffset <= offset ? offset : -1;
            }

            public override VisualLineElement ConstructElement(int offset)
            {
                return offset == this.offset ? new TestVisualLineElement(length) : null;
            }
        }

        sealed class TestVisualLineElement : VisualLineElement
        {
            public TestVisualLineElement(int length) : base(length, length)
            {
            }
        }

        sealed class MarkingTransformer : IVisualLineTransformer
        {
            public int SeenElements { get; private set; }
            public int TransformCalls { get; private set; }

            public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
            {
                TransformCalls++;
                SeenElements = elements.Count;
            }
        }
    }
}
