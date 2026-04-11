using System;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.TextMate
{
	/// <summary>
	/// Immutable-ish line snapshot adapter for TextMateSharp.
	/// Derived from the AvaloniaEdit.TextMate implementation, but kept editor-agnostic.
	/// </summary>
	public sealed class DocumentSnapshot
	{
		LineRange[] lineRanges;
		readonly TextDocument document;
		ITextSource textSource;
		readonly object gate = new object();
		int lineCount;

		public int LineCount {
			get { lock (gate) { return lineCount; } }
		}

		public DocumentSnapshot(TextDocument document)
		{
			this.document = document ?? throw new ArgumentNullException(nameof(document));
			lineRanges = new LineRange[document.LineCount];
			Update(null);
		}

		public void RemoveLines(int startLine, int endLine)
		{
			lock (gate) {
				var tmpList = lineRanges.ToList();
				tmpList.RemoveRange(startLine, endLine - startLine + 1);
				lineRanges = tmpList.ToArray();
				lineCount = lineRanges.Length;
			}
		}

		public string GetLineText(int lineIndex)
		{
			lock (gate) {
				LineRange lineRange = lineRanges[lineIndex];
				return textSource.GetText(lineRange.Offset, lineRange.Length);
			}
		}

		public string GetLineTextIncludingTerminator(int lineIndex)
		{
			lock (gate) {
				LineRange lineRange = lineRanges[lineIndex];
				return textSource.GetText(lineRange.Offset, lineRange.TotalLength);
			}
		}

		public string GetLineTerminator(int lineIndex)
		{
			lock (gate) {
				LineRange lineRange = lineRanges[lineIndex];
				return textSource.GetText(lineRange.Offset + lineRange.Length, lineRange.TotalLength - lineRange.Length);
			}
		}

		public int GetLineLength(int lineIndex)
		{
			lock (gate) {
				return lineRanges[lineIndex].Length;
			}
		}

		public int GetTotalLineLength(int lineIndex)
		{
			lock (gate) {
				return lineRanges[lineIndex].TotalLength;
			}
		}

		public void Update(DocumentChangeEventArgs e)
		{
			lock (gate) {
				lineCount = document.Lines.Count;

				if (e != null && e.OffsetChangeMap != null && lineRanges != null && lineCount == lineRanges.Length) {
					RecalculateOffsets(e);
				} else {
					RecomputeAllLineRanges(e);
				}

				textSource = document.CreateSnapshot();
			}
		}

		void RecalculateOffsets(DocumentChangeEventArgs e)
		{
			DocumentLine changedLine = document.GetLineByOffset(e.Offset);
			int lineIndex = changedLine.LineNumber - 1;

			lineRanges[lineIndex].Offset = changedLine.Offset;
			lineRanges[lineIndex].Length = changedLine.Length;
			lineRanges[lineIndex].TotalLength = changedLine.TotalLength;

			for (int i = lineIndex + 1; i < lineCount; i++) {
				lineRanges[i].Offset = e.OffsetChangeMap.GetNewOffset(lineRanges[i].Offset);
			}
		}

		void RecomputeAllLineRanges(DocumentChangeEventArgs e)
		{
			Array.Resize(ref lineRanges, lineCount);

			int currentLineIndex = e != null
				? document.GetLineByOffset(e.Offset).LineNumber - 1
				: 0;
			DocumentLine currentLine = document.GetLineByNumber(currentLineIndex + 1);

			while (currentLine != null) {
				lineRanges[currentLineIndex].Offset = currentLine.Offset;
				lineRanges[currentLineIndex].Length = currentLine.Length;
				lineRanges[currentLineIndex].TotalLength = currentLine.TotalLength;
				currentLine = currentLine.NextLine;
				currentLineIndex++;
			}
		}

		struct LineRange
		{
			public int Offset;
			public int Length;
			public int TotalLength;
		}
	}
}
