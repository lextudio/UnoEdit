using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class InputHandlerRuntimeTests
{
    [TestMethod]
    public async Task CaretNavigationHandler_Right_MovesCaret()
    {
        var document = new TextDocument("abcdef");
        var editor = new TextEditor { Document = document };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        TextView textView = editor.TextArea.TextView;
        textView.CurrentOffset = 0;
        await UnitTestsUIContentHelper.WaitForIdle();

        bool handled = CaretNavigationCommandHandler.HandleKeyDown(
            textView,
            Windows.System.VirtualKey.Right,
            controlPressed: false,
            extendSelection: false);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, textView.CurrentOffset);
    }

    [TestMethod]
    public async Task EditingCommandHandler_SelectAll_SelectsWholeDocument()
    {
        var document = new TextDocument("hello world");
        var editor = new TextEditor { Document = document };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        TextView textView = editor.TextArea.TextView;
        bool handled = await EditingCommandHandler.HandleKeyDownAsync(
            textView,
            Windows.System.VirtualKey.A,
            controlPressed: true,
            extendSelection: false);

        Assert.IsTrue(handled);
        Assert.AreEqual(0, textView.SelectionStartOffset);
        Assert.AreEqual(document.TextLength, textView.SelectionEndOffset);
        Assert.AreEqual(document.TextLength, textView.CurrentOffset);
    }

    [TestMethod]
    public async Task CaretNavigationHandler_CtrlM_TogglesFoldAtCaret()
    {
        var document = new TextDocument("{\n  one\n  two\n}");
        var foldingManager = new FoldingManager(document);
        DocumentLine firstLine = document.GetLineByNumber(1);
        DocumentLine lastLine = document.GetLineByNumber(4);
        var fold = foldingManager.CreateFolding(firstLine.EndOffset, lastLine.EndOffset);

        var editor = new TextEditor
        {
            Document = document,
            FoldingManager = foldingManager,
        };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        TextView textView = editor.TextArea.TextView;
        textView.CurrentOffset = firstLine.Offset;
        await UnitTestsUIContentHelper.WaitForIdle();

        bool handled = CaretNavigationCommandHandler.HandleKeyDown(
            textView,
            Windows.System.VirtualKey.M,
            controlPressed: true,
            extendSelection: false);

        Assert.IsTrue(handled);
        Assert.IsTrue(fold.IsFolded);
    }
}
