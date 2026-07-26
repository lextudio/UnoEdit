using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.TextMate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TextMateSharp.Grammars;
using Uno.UI.RuntimeTests;
using UnoEdit.Skia.Desktop;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class TextMateSwitchRuntimeTests
{
    [TestMethod]
    public async Task SwitchingFromXshdToTextMate_HighlightsWithoutScrolling()
    {
        var document = new TextDocument("""
            using System;

            namespace Demo;

            public static class Program
            {
                public static void Main()
                {
                    Console.WriteLine("Hello");
                }
            }
            """);
        var editor = new TextEditor { Document = document };
        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        var definition = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
        Assert.IsNotNull(definition);
        editor.HighlightedLineSource = new XshdHighlightedLineSource(definition);
        await UnitTestsUIContentHelper.WaitForIdle();
        Assert.IsTrue(HasColoredVisibleRuns(editor),
            "The XSHD baseline should contain colored visible runs.");

        using var textMate = new TextMateLineHighlighter(new RegistryOptions(ThemeName.DarkPlus));
        textMate.SetGrammarByExtension(".cs");
        editor.HighlightedLineSource = textMate;

        // Do not scroll or otherwise invalidate the viewport. The token-completion
        // notification must repaint the frame created by the highlighter switch.
        var deadline = System.DateTime.UtcNow + System.TimeSpan.FromSeconds(2);
        while (!HasColoredVisibleRuns(editor) && System.DateTime.UtcNow < deadline)
        {
            await System.Threading.Tasks.Task.Delay(20);
            await UnitTestsUIContentHelper.WaitForIdle();
        }

        var visibleColors = editor.TextArea.TextView.VisibleLineViewModels
            .SelectMany(line => line.Runs)
            .Select(run => run.Foreground.ToString())
            .Distinct()
            .ToArray();
        var directLine = textMate.HighlightLine(1);
        var directColors = directLine?.Sections
            .Select(section => section.Color.Foreground?.GetColor()?.ToString() ?? "<none>")
            .ToArray() ?? [];
        Assert.IsTrue(HasColoredVisibleRuns(editor),
            $"TextMate completed, but the switched viewport was not repainted until scrolling. " +
            $"Visible colors: {string.Join(", ", visibleColors)}; direct colors: {string.Join(", ", directColors)}.");
    }

    private static bool HasColoredVisibleRuns(TextEditor editor)
    {
        var defaultColor = editor.Theme.DefaultForeground;
        return editor.TextArea.TextView.VisibleLineViewModels
            .SelectMany(line => line.Runs)
            .Any(run => run.Foreground != defaultColor);
    }
}
