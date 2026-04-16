using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Snippets;
using NUnit.Framework;

namespace UnoEdit.Tests.Snippets;

[TestFixture]
public class SnippetTypesTests
{
    sealed class FakeTextArea
    {
        public TextDocument Document { get; } = new("alpha");
        public int CurrentOffset { get; set; }
        public string SelectedText { get; set; } = string.Empty;
        public TextEditorOptions Options { get; } = new();
    }

    [Test]
    public void SnippetElement_ToTextRun_UsesTypeBasedDescriptor()
    {
        var element = new SnippetCaretElement();

        var run = (SnippetElement.SnippetTextRun)element.ToTextRun();

        Assert.That(run.Kind, Is.EqualTo("caret"));
    }

    [Test]
    public void AnchorElement_TracksInsertionCompletionAndDeactivation()
    {
        var textArea = new FakeTextArea();
        var context = new InsertionContext(textArea, 0);
        var snippet = new SnippetAnchorElement("anchor");

        snippet.Insert(context);
        var anchor = (AnchorElement)context.GetActiveElement(snippet)!;

        Assert.That(anchor.IsEditable, Is.False);

        context.RaiseInsertionCompleted(EventArgs.Empty);
        Assert.That(anchor.IsEditable, Is.True);
        anchor.Deactivate(new SnippetEventArgs(DeactivateReason.Deleted));

        Assert.That(anchor.IsEditable, Is.False);
        Assert.That(anchor.Name, Is.EqualTo(string.Empty));
    }
}
