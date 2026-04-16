using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.TextMate;
using NUnit.Framework;

namespace UnoEdit.Tests.TextMate;

[TestFixture]
public class TextDocumentLineListTests
{
    [Test]
    public void UpdateLine_RefreshesSnapshotForRequestedLine()
    {
        var document = new TextDocument("one\ntwo");
        using var lines = new TextDocumentLineList(document);

        document.Replace(4, 3, "changed");
        lines.UpdateLine(1);

        Assert.That(lines.GetLineTextIncludingTerminators(1).ToString(), Is.EqualTo("changed"));
    }
}
