using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.TextMate;
using Microsoft.UI.Dispatching;
using NUnit.Framework;
using TextMateSharp.Grammars;

namespace UnoEdit.Tests.TextMate
{
    [TestFixture]
    public class TextMateLineHighlighterTests
    {
        class MockTextView : ITextView
        {
            public event EventHandler VisibleLinesChanged;
            public event EventHandler ScrollOffsetChanged;
            public int FirstVisibleLineNumber { get; init; }
            public int LastVisibleLineNumber { get; init; } = int.MaxValue;
            public DispatcherQueue DispatcherQueue => null;
        }

        [Test]
        public void HighlightLine_ReturnsSections_ForCSharpKeywords()
        {
            var document = new TextDocument("public static class Demo { }\n");
            var highlighter = new TextMateLineHighlighter(new RegistryOptions(ThemeName.DarkPlus));
            highlighter.SetTextView(new MockTextView());
            highlighter.SetDocument(document);
            highlighter.SetGrammarByExtension(".cs");

            var line = WaitForHighlightedLine(highlighter, 1);

            Assert.That(line, Is.Not.Null);
            Assert.That(line!.Sections.Count, Is.GreaterThan(0));
            Assert.That(line.Sections.Any(s => document.GetText(s.Offset, s.Length) == "public"), Is.True);
        }

        [Test]
        public void ChangingTheme_RaisesInvalidation()
        {
            var document = new TextDocument("public class Demo {}\n");
            var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            var highlighter = new TextMateLineHighlighter(registryOptions);
            highlighter.SetTextView(new MockTextView());
            highlighter.SetDocument(document);
            highlighter.SetGrammarByExtension(".cs");

            int invalidations = 0;
            highlighter.HighlightingInvalidated += (_, _) => invalidations++;

            highlighter.SetTheme(ThemeName.LightPlus);

            Assert.That(invalidations, Is.GreaterThan(0));
        }

        [Test]
        public void HighlightLine_ReusesCachedHighlightedLine_WhenTokensStayStable()
        {
            var document = new TextDocument("public static class Demo { }\n");
            var highlighter = new TextMateLineHighlighter(new RegistryOptions(ThemeName.DarkPlus));
            highlighter.SetTextView(new MockTextView());
            highlighter.SetDocument(document);
            highlighter.SetGrammarByExtension(".cs");

            var first = WaitForHighlightedLine(highlighter, 1);
            var second = highlighter.HighlightLine(1);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void HighlightLine_EventuallyReachesViewportNearEnd_WithoutInvalidationFlood()
        {
            const int lineCount = 2000;
            var text = new StringBuilder("/* multiline comment\n");
            for (int i = 2; i < lineCount; i++)
            {
                text.Append("comment body ").Append(i).Append('\n');
            }
            text.Append("*/ public static class Tail { }\n");

            var document = new TextDocument(text.ToString());
            using var highlighter = new TextMateLineHighlighter(new RegistryOptions(ThemeName.DarkPlus));
            highlighter.SetTextView(new MockTextView
            {
                FirstVisibleLineNumber = lineCount,
                LastVisibleLineNumber = lineCount
            });
            highlighter.SetDocument(document);
            highlighter.SetGrammarByExtension(".cs");

            // Pending reads must be observational. The previous implementation
            // invalidated and force-tokenized this line on every read, flooding
            // the worker queue and starting with an unknown multiline state.
            for (int i = 0; i < 250; i++)
            {
                _ = highlighter.HighlightLine(lineCount);
            }

            var line = WaitForHighlightedLine(highlighter, lineCount, TimeSpan.FromSeconds(10));

            Assert.That(line, Is.Not.Null);
            Assert.That(
                line!.Sections.Any(s => document.GetText(s.Offset, s.Length) == "class"),
                Is.True);
        }

        private static HighlightedLine WaitForHighlightedLine(TextMateLineHighlighter highlighter, int lineNumber)
            => WaitForHighlightedLine(highlighter, lineNumber, TimeSpan.FromSeconds(5));

        private static HighlightedLine WaitForHighlightedLine(
            TextMateLineHighlighter highlighter,
            int lineNumber,
            TimeSpan timeout)
        {
            using var changed = new AutoResetEvent(false);
            void OnChanged(object sender, HighlightedLineRangeInvalidatedEventArgs e)
            {
                if (e.StartLineNumber <= lineNumber && lineNumber <= e.EndLineNumber)
                {
                    changed.Set();
                }
            }

            highlighter.HighlightingRangeInvalidated += OnChanged;
            try
            {
                var deadline = Stopwatch.StartNew();
                while (deadline.Elapsed < timeout)
                {
                    var line = highlighter.HighlightLine(lineNumber);
                    if (line is { Sections.Count: > 0 })
                    {
                        return line;
                    }

                    var remaining = timeout - deadline.Elapsed;
                    if (remaining <= TimeSpan.Zero)
                    {
                        break;
                    }

                    changed.WaitOne(remaining < TimeSpan.FromMilliseconds(100)
                        ? remaining
                        : TimeSpan.FromMilliseconds(100));
                }

                var finalLine = highlighter.HighlightLine(lineNumber);
                return finalLine;
            }
            finally
            {
                highlighter.HighlightingRangeInvalidated -= OnChanged;
            }
        }
    }
}
