using ICSharpCode.AvalonEdit.Editing;
using NUnit.Framework;

namespace UnoEdit.Tests.Editing;

[TestFixture]
public class InputHandlersTests
{
    sealed class TestStackedInputHandler : TextAreaStackedInputHandler
    {
        public TestStackedInputHandler(object textArea) : base(textArea)
        {
        }

        public int Version => PreviewKeyEventVersion;
        public object LastDown => LastPreviewKeyDownEvent;
        public object LastUp => LastPreviewKeyUpEvent;
    }

    [Test]
    public void TextAreaStackedInputHandler_TracksAttachAndPreviewEvents()
    {
        var handler = new TestStackedInputHandler(new object());

        handler.OnPreviewKeyDown("ignored");
        Assert.That(handler.Version, Is.EqualTo(0));

        handler.Attach();
        handler.OnPreviewKeyDown("down");
        handler.OnPreviewKeyUp("up");

        Assert.That(handler.IsAttached, Is.True);
        Assert.That(handler.Version, Is.EqualTo(2));
        Assert.That(handler.LastDown, Is.EqualTo("down"));
        Assert.That(handler.LastUp, Is.EqualTo("up"));

        handler.Detach();

        Assert.That(handler.IsAttached, Is.False);
        Assert.That(handler.LastDown, Is.Null);
        Assert.That(handler.LastUp, Is.Null);
    }
}
