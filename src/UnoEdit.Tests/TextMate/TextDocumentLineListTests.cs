using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.TextMate;
using Microsoft.UI.Dispatching;
using NUnit.Framework;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Tests.TextMate;

[TestFixture]
public class TextDocumentLineListTests
{
    class MockTextView : ITextView
    {
        public event EventHandler VisibleLinesChanged;
        public event EventHandler ScrollOffsetChanged;
        public int FirstVisibleLineNumber => 0;
        public int LastVisibleLineNumber => 0;
        public DispatcherQueue DispatcherQueue => null;
    }

    [Test]
    public void UpdateLine_RefreshesSnapshotForRequestedLine()
    {
        var document = new TextDocument("one\ntwo");
        var textView = new MockTextView();
        using var lines = new TextDocumentLineList(textView, document);

        document.Replace(4, 3, "changed");
        lines.UpdateLine(1);

        Assert.That(lines.GetLineTextIncludingTerminators(1).ToString(), Is.EqualTo("changed"));
    }
}
