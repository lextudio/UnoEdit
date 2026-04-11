using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.TextMate;
using NUnit.Framework;
using TextMateSharp.Grammars;

namespace UnoEdit.Tests.TextMate
{
    [TestFixture]
    public class TextMateLineHighlighterTests
    {
        [Test]
        public void HighlightLine_ReturnsSections_ForCSharpKeywords()
        {
            var document = new TextDocument("public static class Demo { }\n");
            var highlighter = new TextMateLineHighlighter(new RegistryOptions(ThemeName.DarkPlus));
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
            highlighter.SetDocument(document);
            highlighter.SetGrammarByExtension(".cs");

            int invalidations = 0;
            highlighter.HighlightingInvalidated += (_, _) => invalidations++;

            highlighter.SetTheme(ThemeName.LightPlus);

            Assert.That(invalidations, Is.GreaterThan(0));
        }

        private static HighlightedLine WaitForHighlightedLine(TextMateLineHighlighter highlighter, int lineNumber)
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < TimeSpan.FromSeconds(2))
            {
                var line = highlighter.HighlightLine(lineNumber);
                if (line is { Sections.Count: > 0 })
                {
                    return line;
                }

                Thread.Sleep(20);
            }

            return highlighter.HighlightLine(lineNumber);
        }
    }
}
