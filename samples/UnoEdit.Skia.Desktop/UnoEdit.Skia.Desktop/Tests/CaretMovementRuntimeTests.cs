// CaretMovementRuntimeTests — UI thread tests for keyboard navigation.
// Runs headlessly when UNO_RUNTIME_TESTS_RUN_TESTS env var is set.

using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class CaretMovementRuntimeTests
{
    [TestMethod]
    public async Task MoveHorizontal_Right_IncrementsOffset()
    {
        var doc = new TextDocument("abcdef");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var tvType = textView.GetType();

        var currentProp = tvType.GetProperty("CurrentOffset", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(currentProp);
        currentProp.SetValue(textView, 0);
        await UnitTestsUIContentHelper.WaitForIdle();

        var moveHorizontal = tvType.GetMethod("MoveHorizontal", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(moveHorizontal);
        bool result = (bool)moveHorizontal.Invoke(textView, new object[] { 1, false })!;
        Assert.IsTrue(result, "MoveHorizontal should return true.");

        int newOffset = (int)currentProp.GetValue(textView)!;
        Assert.AreEqual(1, newOffset);
    }

    [TestMethod]
    public async Task MoveVertical_Down_MovesToNextLine()
    {
        var doc = new TextDocument("line1\nline2\nline3");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var tvType = textView.GetType();
        var currentProp = tvType.GetProperty("CurrentOffset", BindingFlags.Instance | BindingFlags.Public);
        var moveVertical = tvType.GetMethod("MoveVertical", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(moveVertical);
        Assert.IsNotNull(currentProp);

        // Move to start of line 1
        currentProp.SetValue(textView, doc.GetOffset(1, 1));
        await UnitTestsUIContentHelper.WaitForIdle();

        bool handled = (bool)moveVertical.Invoke(textView, new object[] { 1, false })!;
        Assert.IsTrue(handled);

        int expected = doc.GetOffset(2, 1);
        int newOffset = (int)currentProp.GetValue(textView)!;
        Assert.AreEqual(expected, newOffset);
    }

    [TestMethod]
    public async Task MoveToLineBoundary_End_MovesToLineEnd()
    {
        var doc = new TextDocument("aaa\nbbbb\ncc");
        var editor = new TextEditor { Document = doc };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = GetTextViewInstance(editor);
        var tvType = textView.GetType();
        var currentProp = tvType.GetProperty("CurrentOffset", BindingFlags.Instance | BindingFlags.Public);
        var moveToLineBoundary = tvType.GetMethod("MoveToLineBoundary", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(moveToLineBoundary);
        Assert.IsNotNull(currentProp);

        // Position at start of line 2
        currentProp.SetValue(textView, doc.GetOffset(2, 1));
        await UnitTestsUIContentHelper.WaitForIdle();

        bool handled = (bool)moveToLineBoundary.Invoke(textView, new object[] { false, false })!; // moveToStart=false => end
        Assert.IsTrue(handled);

        int expected = doc.GetOffset(2, doc.GetLineByNumber(2).Length + 1);
        int newOffset = (int)currentProp.GetValue(textView)!;
        Assert.AreEqual(expected, newOffset);
    }

    private static TextView GetTextViewInstance(TextEditor editor)
        => editor.TextArea.TextView;
}
