using System;
using System.Collections.Generic;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering
{
    [TestFixture]
    public class TextViewWeakEventManagerTests
    {
        [Test]
        public void DocumentChangedListener_ReceivesWeakEvent()
        {
            var textView = new TextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.DocumentChanged.AddListener(textView, listener);
            textView.Document = new TextDocument("hello");

            Assert.That(listener.ReceivedManagerTypes, Is.EqualTo(new[] { typeof(TextViewWeakEventManager.DocumentChanged) }));
            Assert.That(listener.LastSender, Is.SameAs(textView));
        }

        [Test]
        public void VisualLinesChangedListener_ReceivesWeakEvent()
        {
            var textView = new TextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.VisualLinesChanged.AddListener(textView, listener);
            textView.Redraw();

            Assert.That(listener.ReceivedManagerTypes, Is.EqualTo(new[] { typeof(TextViewWeakEventManager.VisualLinesChanged) }));
        }

        [Test]
        public void ScrollOffsetChangedListener_StopsReceivingAfterRemove()
        {
            var textView = new TextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.ScrollOffsetChanged.AddListener(textView, listener);
            TextViewWeakEventManager.ScrollOffsetChanged.RemoveListener(textView, listener);
            textView.HorizontalOffset = 10;
            textView.VerticalOffset = 10;
            textView.MakeVisible(new Windows.Foundation.Rect(0, 0, 1, 1));

            Assert.That(listener.ReceivedManagerTypes, Is.Empty);
        }

        sealed class RecordingWeakEventListener : IWeakEventListener
        {
            public List<Type> ReceivedManagerTypes { get; } = new List<Type>();

            public object LastSender { get; private set; }

            public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
            {
                ReceivedManagerTypes.Add(managerType);
                LastSender = sender;
                return true;
            }
        }
    }
}
