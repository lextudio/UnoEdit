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
            var textView = new TestTextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.DocumentChanged.AddListener(textView, listener);
            textView.Document = new TextDocument("hello");

            Assert.That(listener.ReceivedManagerTypes, Is.EqualTo(new[] { typeof(TextViewWeakEventManager.DocumentChanged) }));
            Assert.That(listener.LastSender, Is.SameAs(textView));
        }

        [Test]
        public void VisualLinesChangedListener_ReceivesWeakEvent()
        {
            var textView = new TestTextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.VisualLinesChanged.AddListener(textView, listener);
            textView.RaiseVisualLinesChanged();

            Assert.That(listener.ReceivedManagerTypes, Is.EqualTo(new[] { typeof(TextViewWeakEventManager.VisualLinesChanged) }));
        }

        [Test]
        public void ScrollOffsetChangedListener_StopsReceivingAfterRemove()
        {
            var textView = new TestTextView();
            var listener = new RecordingWeakEventListener();

            TextViewWeakEventManager.ScrollOffsetChanged.AddListener(textView, listener);
            TextViewWeakEventManager.ScrollOffsetChanged.RemoveListener(textView, listener);
            textView.RaiseScrollOffsetChanged();

            Assert.That(listener.ReceivedManagerTypes, Is.Empty);
        }

        sealed class TestTextView : TextView
        {
            public void RaiseVisualLinesChanged()
            {
                OnVisualLinesChanged(EventArgs.Empty);
            }

            public void RaiseScrollOffsetChanged()
            {
                OnScrollOffsetChanged(EventArgs.Empty);
            }
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
