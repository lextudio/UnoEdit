using System;
using ICSharpCode.AvalonEdit.Document;
using TextMateSharp.Grammars;
using TextMateSharp.Model;

namespace ICSharpCode.AvalonEdit.TextMate
{
	/// <summary>
	/// Tracks AvalonEdit document edits for TextMateSharp's TMModel.
	/// Adapted from AvaloniaEdit.TextMate.TextEditorModel, minus viewport-specific logic.
	/// </summary>
	public sealed class TextDocumentLineList : AbstractLineList, IDisposable
	{
		readonly TextDocument document;
		readonly Action<Exception> exceptionHandler;
		DocumentSnapshot documentSnapshot;
		InvalidLineRange invalidRange;

		public DocumentSnapshot DocumentSnapshot {
			get { return documentSnapshot; }
		}

		public TextDocumentLineList(TextDocument document, Action<Exception> exceptionHandler = null)
		{
			this.document = document ?? throw new ArgumentNullException(nameof(document));
			this.exceptionHandler = exceptionHandler;
			documentSnapshot = new DocumentSnapshot(document);

			for (int i = 0; i < document.LineCount; i++)
				AddLine(i);

			document.Changing += DocumentOnChanging;
			document.Changed += DocumentOnChanged;
			document.UpdateFinished += DocumentOnUpdateFinished;
		}

		public override void Dispose()
		{
			document.Changing -= DocumentOnChanging;
			document.Changed -= DocumentOnChanged;
			document.UpdateFinished -= DocumentOnUpdateFinished;
		}

		public override void UpdateLine(int lineIndex)
		{
		}

		public override int GetNumberOfLines()
		{
			return documentSnapshot.LineCount;
		}

		public override LineText GetLineTextIncludingTerminators(int lineIndex)
		{
			return new LineText(documentSnapshot.GetLineTextIncludingTerminator(lineIndex));
		}

		public override int GetLineLength(int lineIndex)
		{
			return documentSnapshot.GetLineLength(lineIndex);
		}

		void DocumentOnChanging(object sender, DocumentChangeEventArgs e)
		{
			try {
				if (e.RemovalLength > 0) {
					int startLine = document.GetLineByOffset(e.Offset).LineNumber - 1;
					int endLine = document.GetLineByOffset(e.Offset + e.RemovalLength).LineNumber - 1;

					for (int i = endLine; i > startLine; i--) {
						RemoveLine(i);
					}

					documentSnapshot.RemoveLines(startLine, endLine);
				}
			} catch (Exception ex) {
				exceptionHandler?.Invoke(ex);
			}
		}

		void DocumentOnChanged(object sender, DocumentChangeEventArgs e)
		{
			try {
				int startLine = document.GetLineByOffset(e.Offset).LineNumber - 1;
				int endLine = startLine;
				if (e.InsertionLength > 0) {
					endLine = document.GetLineByOffset(e.Offset + e.InsertionLength).LineNumber - 1;
					for (int i = startLine; i < endLine; i++) {
						AddLine(i);
					}
				}

				documentSnapshot.Update(e);

				if (startLine == 0) {
					SetInvalidRange(startLine, endLine);
					return;
				}

				SetInvalidRange(startLine - 1, endLine);
			} catch (Exception ex) {
				exceptionHandler?.Invoke(ex);
			}
		}

		void SetInvalidRange(int startLine, int endLine)
		{
			if (!document.IsInUpdate) {
				InvalidateLineRange(startLine, endLine);
				return;
			}

			if (invalidRange == null) {
				invalidRange = new InvalidLineRange(startLine, endLine);
				return;
			}

			invalidRange.SetInvalidRange(startLine, endLine);
		}

		void DocumentOnUpdateFinished(object sender, EventArgs e)
		{
			if (invalidRange == null)
				return;

			try {
				int startLine = Math.Clamp(invalidRange.StartLine, 0, documentSnapshot.LineCount - 1);
				int endLine = Math.Clamp(invalidRange.EndLine, 0, documentSnapshot.LineCount - 1);
				InvalidateLineRange(startLine, endLine);
			} finally {
				invalidRange = null;
			}
		}

		sealed class InvalidLineRange
		{
			public int StartLine { get; private set; }
			public int EndLine { get; private set; }

			public InvalidLineRange(int startLine, int endLine)
			{
				StartLine = startLine;
				EndLine = endLine;
			}

			public void SetInvalidRange(int startLine, int endLine)
			{
				if (startLine < StartLine)
					StartLine = startLine;
				if (endLine > EndLine)
					EndLine = endLine;
			}
		}
	}
}
