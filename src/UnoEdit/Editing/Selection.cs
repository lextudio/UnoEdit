// UnoEdit port of ICSharpCode.AvalonEdit.Editing.Selection and RectangleSelection.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Abstract base class for text selections.
	/// </summary>
	public abstract class Selection
	{
		/// <summary>Creates a new selection from startOffset to endOffset.</summary>
		public static Selection Create(object textArea, int startOffset, int endOffset)
		{
			if (textArea == null) throw new System.ArgumentNullException(nameof(textArea));
			if (startOffset == endOffset) return new SimpleSelection(textArea, startOffset, startOffset);
			return new SimpleSelection(textArea, startOffset, endOffset);
		}

		/// <summary>Creates a new selection for the specified segment.</summary>
		public static Selection Create(object textArea, ISegment segment)
		{
			if (segment == null) throw new System.ArgumentNullException(nameof(segment));
			return Create(textArea, segment.Offset, segment.EndOffset);
		}

		/// <summary>Gets the start position of the selection.</summary>
		public abstract TextViewPosition StartPosition { get; }

		/// <summary>Gets the end position of the selection.</summary>
		public abstract TextViewPosition EndPosition { get; }

		/// <summary>Gets the selection segments.</summary>
		public abstract IEnumerable<SelectionSegment> Segments { get; }

		/// <summary>Gets the surrounding segment (smallest segment containing all selection segments).</summary>
		public abstract ISegment SurroundingSegment { get; }

		/// <summary>Replaces the selection with the specified text.</summary>
		public abstract void ReplaceSelectionWithText(string newText);

		/// <summary>Updates the selection after a document change.</summary>
		public abstract Selection UpdateOnDocumentChange(DocumentChangeEventArgs e);

		/// <summary>Gets whether the selection is empty.</summary>
		public virtual bool IsEmpty => Length == 0;

		/// <summary>Gets whether virtual space is enabled for this selection.</summary>
		public virtual bool EnableVirtualSpace => false;

		/// <summary>Gets the selection length.</summary>
		public abstract int Length { get; }

		/// <summary>Returns a new selection with the changed end point.</summary>
		public abstract Selection SetEndpoint(TextViewPosition endPosition);

		/// <summary>Starts a new selection or updates the endpoint.</summary>
		public abstract Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition);

		/// <summary>Gets whether the selection spans multiple lines.</summary>
		public virtual bool IsMultiline => false;

		/// <summary>Gets the selected text.</summary>
		public virtual string GetText() => string.Empty;

		/// <summary>Gets whether the selection contains the specified offset.</summary>
		public virtual bool Contains(int offset) => false;

		/// <summary>Creates a data object for clipboard operations.</summary>
		public virtual object CreateDataObject(object textArea)
		{
			var data = new DataObject();
			string text = GetText();
			if (!string.IsNullOrEmpty(text))
			{
				data.SetData(DataFormats.Text, text);
				data.SetData(DataFormats.UnicodeText, text);
			}
			return data;
		}

		/// <summary>Creates an HTML fragment for the selection (stub).</summary>
		public virtual string CreateHtmlFragment(object options)
		{
			string text = GetText();
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			string escaped = text
				.Replace("&", "&amp;", StringComparison.Ordinal)
				.Replace("<", "&lt;", StringComparison.Ordinal)
				.Replace(">", "&gt;", StringComparison.Ordinal)
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace("\r", "\n", StringComparison.Ordinal)
				.Replace("\n", "<br>", StringComparison.Ordinal);
			return "<!--StartFragment-->" + escaped + "<!--EndFragment-->";
		}

		/// <summary>Determines whether the specified object equals this selection.</summary>
		public abstract override bool Equals(object obj);

		/// <summary>Gets the hash code for this selection.</summary>
		public abstract override int GetHashCode();
	}

	/// <summary>
	/// Represents a rectangular (box) selection.
	/// </summary>
	public sealed class RectangleSelection : Selection
	{
		/// <summary>The clipboard data format for rectangular selections.</summary>
		public static readonly string RectangularSelectionDataType = "Avalonedit.RectangularSelection";

		private readonly object _textArea;
		private readonly TextViewPosition _start;
		private readonly TextViewPosition _end;

		/// <summary>Creates a new RectangleSelection.</summary>
		public RectangleSelection(object textArea, TextViewPosition start, TextViewPosition end)
		{
			_textArea = textArea;
			_start = start;
			_end = end;
		}

		/// <inheritdoc/>
		public override TextViewPosition StartPosition => _start.CompareTo(_end) <= 0 ? _start : _end;
		/// <inheritdoc/>
		public override TextViewPosition EndPosition => _start.CompareTo(_end) <= 0 ? _end : _start;
		/// <inheritdoc/>
		public override IEnumerable<SelectionSegment> Segments => System.Linq.Enumerable.Empty<SelectionSegment>();
		/// <inheritdoc/>
		public override ISegment SurroundingSegment
		{
			get
			{
				if (!TryGetDocument(_textArea, out var doc))
					return null;

				int startOffset = GetOffsetFromPosition(doc, StartPosition);
				int endOffset = GetOffsetFromPosition(doc, EndPosition);
				if (startOffset == endOffset)
					return null;

				return new SimpleSegment(Math.Min(startOffset, endOffset), Math.Abs(endOffset - startOffset));
			}
		}
		/// <inheritdoc/>
		public override int Length
		{
			get
			{
				var segment = SurroundingSegment;
				return segment?.Length ?? 0;
			}
		}
		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(string newText) { }
		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e) => this;
		/// <inheritdoc/>
		public override Selection SetEndpoint(TextViewPosition endPosition) => this;
		/// <inheritdoc/>
		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition) => this;

		/// <summary>Selects left by one character.</summary>
		public RectangleSelection BoxSelectLeftByCharacter() => this;
		/// <summary>Selects right by one character.</summary>
		public RectangleSelection BoxSelectRightByCharacter() => this;
		/// <summary>Selects left by one word.</summary>
		public RectangleSelection BoxSelectLeftByWord() => this;
		/// <summary>Selects right by one word.</summary>
		public RectangleSelection BoxSelectRightByWord() => this;
		/// <summary>Selects up by one line.</summary>
		public RectangleSelection BoxSelectUpByLine() => this;
		/// <summary>Selects down by one line.</summary>
		public RectangleSelection BoxSelectDownByLine() => this;
		/// <summary>Selects to line start.</summary>
		public RectangleSelection BoxSelectToLineStart() => this;
		/// <summary>Selects to line end.</summary>
		public RectangleSelection BoxSelectToLineEnd() => this;

		/// <summary>Performs a rectangular paste.</summary>
		public static void PerformRectangularPaste(object textArea, TextViewPosition startPosition, string text, bool selectInsertedText) { }

		/// <inheritdoc/>
		public override string GetText()
		{
			if (!TryGetDocument(_textArea, out var doc))
				return string.Empty;

			var segment = SurroundingSegment;
			if (segment == null || segment.Length <= 0)
				return string.Empty;

			return doc.GetText(segment.Offset, segment.Length);
		}
		/// <inheritdoc/>
		public override object CreateDataObject(object textArea)
		{
			var data = new DataObject();
			string text = NormalizeNewlines(GetText());
			data.SetData(DataFormats.Text, text);
			data.SetData(DataFormats.UnicodeText, text);
			data.SetData(RectangularSelectionDataType, bool.TrueString);
			return data;
		}
		/// <inheritdoc/>
		public override bool EnableVirtualSpace => false;

		/// <inheritdoc/>
		public override bool Equals(object obj) => ReferenceEquals(this, obj);
		/// <inheritdoc/>
		public override int GetHashCode() => 0;
		/// <inheritdoc/>
		public override string ToString() => $"RectangleSelection {{ Start={StartPosition}, End={EndPosition} }}";

		private static bool TryGetDocument(object textArea, out TextDocument document)
		{
			document = textArea?.GetType().GetProperty("Document")?.GetValue(textArea) as TextDocument;
			return document != null;
		}

		private static int GetOffsetFromPosition(TextDocument doc, TextViewPosition position)
		{
			int line = Math.Clamp(position.Line, 1, doc.LineCount);
			DocumentLine documentLine = doc.GetLineByNumber(line);
			int column = Math.Clamp(position.Column, 1, documentLine.Length + 1);
			return doc.GetOffset(line, column);
		}

		private static string NormalizeNewlines(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			return text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace("\r", "\n", StringComparison.Ordinal)
				.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
		}
	}

	/// <summary>
	/// Simple (non-rectangular) selection backed by start/end offsets.
	/// </summary>
	public sealed class SimpleSelection : Selection
	{
		private readonly object _textArea;
		private readonly int _startOffset;
		private readonly int _endOffset;

		/// <summary>Creates a new SimpleSelection.</summary>
		public SimpleSelection(object textArea, int startOffset, int endOffset)
		{
			_textArea = textArea;
			_startOffset = startOffset;
			_endOffset   = endOffset;
		}

		/// <inheritdoc/>
		public override TextViewPosition EndPosition
		{
			get
			{
				if (TryGetDocument(_textArea, out var doc))
				{
					int start = Math.Clamp(Math.Min(_startOffset, _endOffset), 0, doc.TextLength);
					int end = Math.Clamp(Math.Max(_startOffset, _endOffset), 0, doc.TextLength);
					return new TextViewPosition(doc.GetLocation(end));
				}

				return new TextViewPosition(1, 1);
			}
		}

		/// <inheritdoc/>
		public override TextViewPosition StartPosition
		{
			get
			{
				if (TryGetDocument(_textArea, out var doc))
				{
					int start = Math.Clamp(Math.Min(_startOffset, _endOffset), 0, doc.TextLength);
					return new TextViewPosition(doc.GetLocation(start));
				}

				return new TextViewPosition(1, 1);
			}
		}

		/// <inheritdoc/>
		public override IEnumerable<SelectionSegment> Segments
		{
			get
			{
				if (_startOffset != _endOffset)
					yield return new SelectionSegment(_startOffset, _endOffset);
			}
		}

		/// <inheritdoc/>
		public override ISegment SurroundingSegment
			=> _startOffset == _endOffset ? null : new SimpleSegment(_startOffset, _endOffset - _startOffset);

		/// <inheritdoc/>
		public override int Length => System.Math.Abs(_endOffset - _startOffset);

		/// <inheritdoc/>
		public override bool IsEmpty => _startOffset == _endOffset;

		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(string newText)
		{
			if (!TryGetDocument(_textArea, out var doc))
				return;

			int start = Math.Clamp(Math.Min(_startOffset, _endOffset), 0, doc.TextLength);
			int end = Math.Clamp(Math.Max(_startOffset, _endOffset), 0, doc.TextLength);
			using (doc.RunUpdate())
			{
				doc.Replace(start, end - start, newText ?? string.Empty);
			}
		}

		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			if (e == null)
				return this;

			int newStart = e.GetNewOffset(_startOffset);
			int newEnd = e.GetNewOffset(_endOffset);
			return new SimpleSelection(_textArea, newStart, newEnd);
		}

		/// <inheritdoc/>
		public override Selection SetEndpoint(TextViewPosition endPosition)
		{
			if (!TryGetDocument(_textArea, out var doc))
				return this;

			int fixedStart = Math.Clamp(_startOffset, 0, doc.TextLength);
			int newEnd = GetOffsetFromPosition(doc, endPosition);
			return new SimpleSelection(_textArea, fixedStart, newEnd);
		}

		/// <inheritdoc/>
		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
		{
			if (!TryGetDocument(_textArea, out var doc))
				return this;

			int startOffset = GetOffsetFromPosition(doc, startPosition);
			int endOffset = GetOffsetFromPosition(doc, endPosition);
			return new SimpleSelection(_textArea, startOffset, endOffset);
		}

		/// <inheritdoc/>
		public override object CreateDataObject(object textArea)
		{
			var data = new DataObject();
			string text = NormalizeNewlines(GetText());
			data.SetData(DataFormats.Text, text);
			data.SetData(DataFormats.UnicodeText, text);
			data.SetData(typeof(string).FullName, text);

			string html = CreateHtmlFragment(null);
			if (!string.IsNullOrEmpty(html))
			{
				data.SetData(DataFormats.Html, html);
			}

			return data;
		}

		/// <inheritdoc/>
		public override string CreateHtmlFragment(object options)
		{
			if (!TryGetDocument(_textArea, out var doc))
				return base.CreateHtmlFragment(options);

			int start = Math.Clamp(Math.Min(_startOffset, _endOffset), 0, doc.TextLength);
			int end = Math.Clamp(Math.Max(_startOffset, _endOffset), 0, doc.TextLength);
			if (end <= start)
				return string.Empty;

			HtmlOptions htmlOptions = options as HtmlOptions ?? new HtmlOptions();
			return HtmlClipboard.CreateHtmlFragment(doc, null, new SimpleSegment(start, end - start), htmlOptions);
		}

		/// <inheritdoc/>
		public override string GetText()
		{
			if (_textArea != null)
			{
				var doc = _textArea.GetType().GetProperty("Document")?.GetValue(_textArea) as TextDocument;
				if (doc != null)
				{
					int start = System.Math.Max(0, System.Math.Min(_startOffset, doc.TextLength));
					int end   = System.Math.Max(0, System.Math.Min(_endOffset,   doc.TextLength));
					if (start > end) { int t = start; start = end; end = t; }
					return doc.GetText(start, end - start);
				}
			}
			return string.Empty;
		}

		/// <inheritdoc/>
		public override bool Contains(int offset)
		{
			int lo = System.Math.Min(_startOffset, _endOffset);
			int hi = System.Math.Max(_startOffset, _endOffset);
			return offset >= lo && offset < hi;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is SimpleSelection other)
				return _startOffset == other._startOffset && _endOffset == other._endOffset && _textArea == other._textArea;
			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
			=> System.HashCode.Combine(_startOffset, _endOffset);

		private static bool TryGetDocument(object textArea, out TextDocument document)
		{
			document = textArea?.GetType().GetProperty("Document")?.GetValue(textArea) as TextDocument;
			return document != null;
		}

		private static int GetOffsetFromPosition(TextDocument doc, TextViewPosition position)
		{
			int line = Math.Clamp(position.Line, 1, doc.LineCount);
			DocumentLine documentLine = doc.GetLineByNumber(line);
			int column = Math.Clamp(position.Column, 1, documentLine.Length + 1);
			return doc.GetOffset(line, column);
		}

		private static string NormalizeNewlines(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			return text
				.Replace("\r\n", "\n", StringComparison.Ordinal)
				.Replace("\r", "\n", StringComparison.Ordinal)
				.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
		}
	}
}
