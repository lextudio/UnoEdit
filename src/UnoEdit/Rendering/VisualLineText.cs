using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Documents;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Uno-side visual line element representing a plain text span.
	/// It preserves the split/caret/column behaviour from AvalonEdit's shared rendering model
	/// without depending on WPF's text formatting pipeline.
	/// </summary>
	public class VisualLineText : VisualLineElement
	{
		readonly VisualLine parentVisualLine;

		public VisualLine ParentVisualLine {
			get { return parentVisualLine; }
		}

		public VisualLineText(VisualLine parentVisualLine, int length) : base(length, length)
		{
			if (parentVisualLine == null)
				throw new ArgumentNullException(nameof(parentVisualLine));
			this.parentVisualLine = parentVisualLine;
		}

		protected virtual VisualLineText CreateInstance(int length)
		{
			return new VisualLineText(parentVisualLine, length);
		}

		public override bool IsWhitespace(int visualColumn)
		{
			int offset = visualColumn - VisualColumn + parentVisualLine.FirstDocumentLine.Offset + RelativeTextOffset;
			return char.IsWhiteSpace(parentVisualLine.Document.GetCharAt(offset));
		}

		public override bool CanSplit {
			get { return true; }
		}

		public override void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
		{
			if (splitVisualColumn <= VisualColumn || splitVisualColumn >= VisualColumn + VisualLength)
				throw new ArgumentOutOfRangeException(nameof(splitVisualColumn), splitVisualColumn, "Value must be within the element range.");
			if (elements == null)
				throw new ArgumentNullException(nameof(elements));
			if (elements[elementIndex] != this)
				throw new ArgumentException("Invalid elementIndex - couldn't find this element at the index.", nameof(elementIndex));

			int relativeSplitPos = splitVisualColumn - VisualColumn;
			VisualLineText splitPart = CreateInstance(DocumentLength - relativeSplitPos);
			SplitHelper(this, splitPart, splitVisualColumn, relativeSplitPos + RelativeTextOffset);
			elements.Insert(elementIndex + 1, splitPart);
		}

		public override int GetRelativeOffset(int visualColumn)
		{
			return RelativeTextOffset + visualColumn - VisualColumn;
		}

		public override int GetVisualColumn(int relativeTextOffset)
		{
			return VisualColumn + relativeTextOffset - RelativeTextOffset;
		}

		public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			int textOffset = parentVisualLine.StartOffset + RelativeTextOffset;
			int position = TextUtilities.GetNextCaretPosition(parentVisualLine.Document, textOffset + visualColumn - VisualColumn, direction, mode);
			if (position < textOffset || position > textOffset + DocumentLength)
				return -1;
			return VisualColumn + position - textOffset;
		}
	}
}
