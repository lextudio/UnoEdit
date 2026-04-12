using System;
using ICSharpCode.AvalonEdit.Rendering;
using NUnit.Framework;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class TextViewCursorTests
{
    [Test]
    public void InvalidateCursor_RaisesSharedNotification()
    {
        int beforeVersion = TextView.CursorInvalidationVersion;
        int eventCount = 0;

        EventHandler handler = (_, _) => eventCount++;
        TextView.CursorInvalidated += handler;
        try
        {
            TextView.InvalidateCursor();
        }
        finally
        {
            TextView.CursorInvalidated -= handler;
        }

        Assert.That(TextView.CursorInvalidationVersion, Is.EqualTo(beforeVersion + 1));
        Assert.That(eventCount, Is.EqualTo(1));
    }
}
