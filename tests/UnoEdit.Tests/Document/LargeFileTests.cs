using System;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using NUnit.Framework;

namespace UnoEdit.Tests.Document
{
    /// <summary>
    /// Smoke tests that verify the portable document core handles large files reliably.
    /// These mirror the kind of load that a large source file opened in ILSpy would put
    /// on the document model.
    /// </summary>
    [TestFixture]
    public class LargeFileTests
    {
        private const int LargeLineCount = 10_000;
        private const int LargeFileCharCount = 100_000;

        private static TextDocument BuildLargeDocument(int lineCount)
        {
            var sb = new StringBuilder(lineCount * 60);
            for (int i = 1; i <= lineCount; i++)
            {
                sb.Append("// Line ");
                sb.Append(i);
                sb.Append(": public static string Method");
                sb.Append(i);
                sb.Append("() => \"hello\";\n");
            }

            return new TextDocument(sb.ToString());
        }

        [Test]
        public void LargeDocument_LineCountIsCorrect()
        {
            var doc = BuildLargeDocument(LargeLineCount);
            // Each AppendLine adds a trailing newline, so N lines of content produce N+1
            // document lines (the last being an empty line after the final newline).
            Assert.That(doc.LineCount, Is.EqualTo(LargeLineCount + 1));
        }

        [Test]
        public void LargeDocument_LineByNumberRoundTrip()
        {
            var doc = BuildLargeDocument(LargeLineCount);

            // Spot-check first, middle and last lines
            foreach (int lineNumber in new[] { 1, LargeLineCount / 2, LargeLineCount })
            {
                DocumentLine line = doc.GetLineByNumber(lineNumber);
                Assert.That(line.LineNumber, Is.EqualTo(lineNumber));
                Assert.That(line.Offset, Is.GreaterThanOrEqualTo(0));
                Assert.That(line.Offset + line.TotalLength, Is.LessThanOrEqualTo(doc.TextLength));
            }
        }

        [Test]
        public void LargeDocument_OffsetToLocationRoundTrip()
        {
            var doc = BuildLargeDocument(LargeLineCount);

            // Pick a small sample of positions across the document
            int step = doc.TextLength / 200;
            for (int offset = 0; offset <= doc.TextLength; offset += step)
            {
                TextLocation loc = doc.GetLocation(offset);
                int roundTripped = doc.GetOffset(loc.Line, loc.Column);
                Assert.That(roundTripped, Is.EqualTo(offset),
                    $"Round-trip failed at offset {offset} (line {loc.Line}, col {loc.Column})");
            }
        }

        [Test]
        public void LargeDocument_GetLineByOffset_AllLineStarts()
        {
            // Every line's Offset must map back to the correct line number via GetLineByOffset.
            var doc = BuildLargeDocument(1_000); // 1 000 lines is enough to exercise the tree

            for (int lineNumber = 1; lineNumber <= doc.LineCount; lineNumber++)
            {
                DocumentLine line = doc.GetLineByNumber(lineNumber);
                DocumentLine fetched = doc.GetLineByOffset(line.Offset);
                Assert.That(fetched.LineNumber, Is.EqualTo(lineNumber));
            }
        }

        [Test]
        public void LargeDocument_InsertAndRemove_LineCountStaysConsistent()
        {
            var doc = BuildLargeDocument(LargeLineCount);
            int originalLineCount = doc.LineCount;

            // Insert a line in the middle
            int midOffset = doc.GetLineByNumber(LargeLineCount / 2).Offset;
            doc.Insert(midOffset, "// INSERTED LINE\n");
            Assert.That(doc.LineCount, Is.EqualTo(originalLineCount + 1));

            // Remove it again
            DocumentLine inserted = doc.GetLineByNumber(LargeLineCount / 2);
            doc.Remove(inserted.Offset, inserted.TotalLength);
            Assert.That(doc.LineCount, Is.EqualTo(originalLineCount));
        }

        [Test]
        public void LargeDocument_UndoRestoresState()
        {
            var doc = BuildLargeDocument(500);
            string snapshot = doc.Text;
            int originalLineCount = doc.LineCount; // 501 (500 content lines + trailing empty line)

            doc.Insert(0, "// prepend\n");
            Assert.That(doc.LineCount, Is.EqualTo(originalLineCount + 1));

            doc.UndoStack.Undo();
            Assert.That(doc.Text, Is.EqualTo(snapshot));
        }

        [Test]
        public void LargeDocument_TextLengthMatchesCharCount()
        {
            // Build a synthetic document of at least LargeFileCharCount characters and verify
            // TextLength equals the string length.
            var sb = new StringBuilder(LargeFileCharCount + 1024);
            int line = 0;
            while (sb.Length < LargeFileCharCount)
            {
                sb.Append($"public void Method{line++}() {{ /* body */ }}\n");
            }

            string text = sb.ToString();
            var doc = new TextDocument(text);
            Assert.That(doc.TextLength, Is.EqualTo(text.Length));
        }

        [Test]
        public void LargeDocument_GetText_AllLines_NoCrash()
        {
            var doc = BuildLargeDocument(2_000);

            // Reading every line's text must not throw and must return a non-null string.
            for (int lineNumber = 1; lineNumber <= doc.LineCount; lineNumber++)
            {
                DocumentLine line = doc.GetLineByNumber(lineNumber);
                string text = doc.GetText(line.Offset, line.Length);
                Assert.That(text, Is.Not.Null);
            }
        }
    }
}
