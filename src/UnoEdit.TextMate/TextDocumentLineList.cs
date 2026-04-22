using System;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.UI.Dispatching;
using TextMateSharp.Grammars;
using TextMateSharp.Model;
using UnoEdit.Logging;
using UnoEdit.Skia.Desktop.Controls;

namespace ICSharpCode.AvalonEdit.TextMate
{
	/// <summary>
	/// Tracks AvalonEdit document edits for TextMateSharp's TMModel.
	/// Adapted from AvaloniaEdit.TextMate.TextEditorModel, minus viewport-specific logic.
	/// </summary>
	public sealed class TextDocumentLineList : AbstractLineList, IDisposable
	{
		static void LogTMModel(string msg) { HighlightLogger.Log("TMModel", msg); }

		readonly TextDocument document;
		readonly TextView textView;
		readonly Action<Exception> exceptionHandler;
		DocumentSnapshot documentSnapshot;
		InvalidLineRange invalidRange;

		public DocumentSnapshot DocumentSnapshot {
			get { return documentSnapshot; }
		}

		public TextDocumentLineList(TextView textView, TextDocument document, Action<Exception> exceptionHandler = null)
		{
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.document = document ?? throw new ArgumentNullException(nameof(document));
			this.exceptionHandler = exceptionHandler;
			documentSnapshot = new DocumentSnapshot(document);

			for (int i = 0; i < document.LineCount; i++)
				AddLine(i);

			document.Changing += DocumentOnChanging;
			document.Changed += DocumentOnChanged;
			document.UpdateFinished += DocumentOnUpdateFinished;
			textView.VisibleLinesChanged += TextView_VisibleLinesChanged;
			textView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
		}

		public override void Dispose()
		{
			document.Changing -= DocumentOnChanging;
			document.Changed -= DocumentOnChanged;
			document.UpdateFinished -= DocumentOnUpdateFinished;
			textView.VisibleLinesChanged -= TextView_VisibleLinesChanged;
			textView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;
		}

		public override void UpdateLine(int lineIndex)
		{
			// Match AvaloniaEdit: TMModel may call UpdateLine while processing tokens.
			// Rebuilding the snapshot or re-invalidating here can perturb parser state.
		}

		public void InvalidateViewPortLines()
		{
			if (!TryGetVisibleLineRange(out int startLineIndex, out int endLineIndex))
				return;

			InvalidateLineRange(startLineIndex, endLineIndex);
		}

		public override int GetNumberOfLines()
		{
			return documentSnapshot.LineCount;
		}

		public override LineText GetLineTextIncludingTerminators(int lineIndex)
		{
			string text = documentSnapshot.GetLineTextIncludingTerminator(lineIndex);
			if (HighlightLogger.Enabled && (lineIndex < 5 || lineIndex + 1 == textView.FirstVisibleLineNumber)) {
				LogTMModel($"GetLineTextIncludingTerminators lineIndex={lineIndex} len={text.Length} text={EscapeForLog(text)}");
			}
			return new LineText(text);
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
				LogTMModel($"DocumentOnChanged offset={e.Offset} insertion={e.InsertionLength} removal={e.RemovalLength} startLine={startLine} endLine={endLine} lineCount={documentSnapshot.LineCount}");

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
			LogTMModel($"SetInvalidRange startLine={startLine} endLine={endLine} inUpdate={document.IsInUpdate}");
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

		void TextView_VisibleLinesChanged(object? sender, EventArgs e)
		{
			try {
				TokenizeViewPort();
			} catch (Exception ex) {
				exceptionHandler?.Invoke(ex);
			}
		}

		void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
		{
			try {
				TokenizeViewPort();
			} catch (Exception ex) {
				exceptionHandler?.Invoke(ex);
			}
		}

		void TokenizeViewPort()
		{
			DispatcherQueue? dispatcherQueue = textView.DispatcherQueue;
			if (dispatcherQueue is null) {
				ForceTokenizeVisibleRange();
				return;
			}

			bool enqueued = dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, ForceTokenizeVisibleRange);
			if (!enqueued) {
				ForceTokenizeVisibleRange();
			}
		}

		void ForceTokenizeVisibleRange()
		{
			if (!TryGetVisibleLineRange(out int startLineIndex, out int endLineIndex))
				return;

			LogTMModel($"ForceTokenizeVisibleRange startLineIndex={startLineIndex} endLineIndex={endLineIndex}");
			ForceTokenization(startLineIndex, endLineIndex);
		}

		bool TryGetVisibleLineRange(out int startLineIndex, out int endLineIndex)
		{
			startLineIndex = -1;
			endLineIndex = -1;

			if (document.LineCount == 0)
				return false;

			int firstVisibleLineNumber = textView.FirstVisibleLineNumber;
			int lastVisibleLineNumber = textView.LastVisibleLineNumber;
			if (firstVisibleLineNumber <= 0 || lastVisibleLineNumber <= 0)
				return false;

			startLineIndex = Math.Clamp(firstVisibleLineNumber - 1, 0, document.LineCount - 1);
			endLineIndex = Math.Clamp(lastVisibleLineNumber - 1, 0, document.LineCount - 1);
			if (endLineIndex < startLineIndex)
				return false;

			return true;
		}

		static string EscapeForLog(string text)
		{
			if (text == null)
				return "<null>";

			string escaped = text
				.Replace("\\", "\\\\")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n");
			if (escaped.Length > 120)
				return escaped.Substring(0, 120) + "...";
			return escaped;
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
