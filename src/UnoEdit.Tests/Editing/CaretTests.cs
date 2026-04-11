using ICSharpCode.AvalonEdit.Editing;
using NUnit.Framework;
using Windows.Foundation;

namespace UnoEdit.Tests.Editing;

[TestFixture]
public class CaretTests
{
    [Test]
    public void CalculateCaretRectangle_UsesInjectedCallback()
    {
        var expected = new Rect(10, 20, 2, 18);
        var caret = new Caret(
            bringIntoView: () => { },
            setOffset: _ => { },
            calculateRectangle: () => expected);

        Assert.That(caret.CalculateCaretRectangle(), Is.EqualTo(expected));
    }

    [Test]
    public void ShowAndHide_ForwardVisibilityChanges()
    {
        bool? visible = null;
        var caret = new Caret(
            bringIntoView: () => { },
            setOffset: _ => { },
            setVisibility: isVisible => visible = isVisible);

        caret.Hide();
        Assert.That(visible, Is.False);

        caret.Show();
        Assert.That(visible, Is.True);
    }
}
