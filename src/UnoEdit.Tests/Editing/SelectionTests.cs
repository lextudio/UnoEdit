using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using NUnit.Framework;

namespace UnoEdit.Tests.Editing;

[TestFixture]
public class SelectionTests
{
    private sealed class FakeTextArea
    {
        public TextDocument Document { get; } = new TextDocument();
        public TextEditorOptions Options { get; } = new TextEditorOptions();
    }

    [Test]
    public void SimpleSelection_ComputesTextContainsAndMultiline()
    {
        var textArea = new FakeTextArea();
        textArea.Document.Text = "alpha\nbeta\ngamma";

        Selection selection = Selection.Create(textArea, 2, 10);

        Assert.That(selection.GetText(), Is.EqualTo("pha\nbeta"));
        Assert.That(selection.Contains(2), Is.True);
        Assert.That(selection.Contains(10), Is.True);
        Assert.That(selection.Contains(11), Is.False);
        Assert.That(selection.IsMultiline, Is.True);
    }

    [Test]
    public void SimpleSelection_CreateHtmlFragment_UsesDocumentHtmlClipboardPath()
    {
        var textArea = new FakeTextArea();
        textArea.Document.Text = "if (x < 1)\n";

        Selection selection = Selection.Create(textArea, 0, 10);
        string html = selection.CreateHtmlFragment(new HtmlOptions());

        Assert.That(html, Does.Contain("StartFragment"));
        Assert.That(html, Does.Contain("if"));
        Assert.That(html, Does.Contain("&lt;"));
    }

    [Test]
    public void RectangleSelection_SegmentsAndText_AreLineBased()
    {
        var textArea = new FakeTextArea();
        textArea.Document.Text = "abcd\nefgh\nijkl";

        var selection = new RectangleSelection(
            textArea,
            new TextViewPosition(1, 2, 1),
            new TextViewPosition(3, 4, 3));

        Assert.That(selection.GetText(), Is.EqualTo("bc\nfg\njk"));
        Assert.That(selection.IsMultiline, Is.True);
        Assert.That(selection.EnableVirtualSpace, Is.True);
    }

    [Test]
    public void RectangleSelection_ReplaceSelectionWithText_ReplacesEachLineSlice()
    {
        var textArea = new FakeTextArea();
        textArea.Document.Text = "abcd\nefgh\nijkl";

        var selection = new RectangleSelection(
            textArea,
            new TextViewPosition(1, 2, 1),
            new TextViewPosition(3, 4, 3));

        selection.ReplaceSelectionWithText("X\nY\nZ");

        Assert.That(textArea.Document.Text, Is.EqualTo("aXd\neYh\niZl"));
    }
}
