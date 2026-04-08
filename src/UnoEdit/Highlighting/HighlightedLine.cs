// Forked from AvalonEdit for UnoEdit — removed HTML/RichText rendering methods.
// Original: ICSharpCode.AvalonEdit/Highlighting/HighlightedLine.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Represents a highlighted document line.
	/// </summary>
	public class HighlightedLine
	{
		/// <summary>
		/// Creates a new HighlightedLine instance.
		/// </summary>
		public HighlightedLine(IDocument document, IDocumentLine documentLine)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.Document = document;
			this.DocumentLine = documentLine;
			this.Sections = new NullSafeCollection<HighlightedSection>();
		}

		/// <summary>Gets the document associated with this HighlightedLine.</summary>
		public IDocument Document { get; private set; }

		/// <summary>Gets the document line associated with this HighlightedLine.</summary>
		public IDocumentLine DocumentLine { get; private set; }

		/// <summary>
		/// Gets the highlighted sections.
		/// The sections are not overlapping, but they may be nested.
		/// In that case, outer sections come in the list before inner sections.
		/// The sections are sorted by start offset.
		/// </summary>
		public IList<HighlightedSection> Sections { get; private set; }

		/// <summary>
		/// Validates that the sections are sorted correctly, and that they are not overlapping.
		/// </summary>
		public void ValidateInvariants()
		{
			var line = this;
			int lineStartOffset = line.DocumentLine.Offset;
			int lineEndOffset = line.DocumentLine.EndOffset;
			for (int i = 0; i < line.Sections.Count; i++) {
				HighlightedSection s1 = line.Sections[i];
				if (s1.Offset < lineStartOffset || s1.Length < 0 || s1.Offset + s1.Length > lineEndOffset)
					throw new InvalidOperationException("Section is outside line bounds");
				for (int j = i + 1; j < line.Sections.Count; j++) {
					HighlightedSection s2 = line.Sections[j];
					if (s2.Offset >= s1.Offset + s1.Length) {
						// s2 is after s1
					} else if (s2.Offset >= s1.Offset && s2.Offset + s2.Length <= s1.Offset + s1.Length) {
						// s2 is nested within s1
					} else {
						throw new InvalidOperationException("Sections are overlapping or incorrectly sorted.");
					}
				}
			}
		}

		#region Merge
		/// <summary>Merges the additional line into this line.</summary>
		public void MergeWith(HighlightedLine additionalLine)
		{
			if (additionalLine == null)
				return;
#if DEBUG
			ValidateInvariants();
			additionalLine.ValidateInvariants();
#endif

			int pos = 0;
			Stack<int> activeSectionEndOffsets = new Stack<int>();
			int lineEndOffset = this.DocumentLine.EndOffset;
			activeSectionEndOffsets.Push(lineEndOffset);
			foreach (HighlightedSection newSection in additionalLine.Sections) {
				int newSectionStart = newSection.Offset;
				while (pos < this.Sections.Count) {
					HighlightedSection s = this.Sections[pos];
					if (newSection.Offset < s.Offset)
						break;
					while (s.Offset > activeSectionEndOffsets.Peek()) {
						activeSectionEndOffsets.Pop();
					}
					activeSectionEndOffsets.Push(s.Offset + s.Length);
					pos++;
				}
				Stack<int> insertionStack = new Stack<int>(activeSectionEndOffsets.Reverse());
				int i;
				for (i = pos; i < this.Sections.Count; i++) {
					HighlightedSection s = this.Sections[i];
					if (newSection.Offset + newSection.Length <= s.Offset)
						break;
					Insert(ref i, ref newSectionStart, s.Offset, newSection.Color, insertionStack);
					while (s.Offset > insertionStack.Peek()) {
						insertionStack.Pop();
					}
					insertionStack.Push(s.Offset + s.Length);
				}
				Insert(ref i, ref newSectionStart, newSection.Offset + newSection.Length, newSection.Color, insertionStack);
			}

#if DEBUG
			ValidateInvariants();
#endif
		}

		void Insert(ref int pos, ref int newSectionStart, int insertionEndPos, HighlightingColor color, Stack<int> insertionStack)
		{
			if (newSectionStart >= insertionEndPos)
				return;

			while (insertionStack.Peek() <= newSectionStart) {
				insertionStack.Pop();
			}
			while (insertionStack.Peek() < insertionEndPos) {
				int end = insertionStack.Pop();
				if (end > newSectionStart) {
					this.Sections.Insert(pos++, new HighlightedSection {
						Offset = newSectionStart,
						Length = end - newSectionStart,
						Color = color
					});
					newSectionStart = end;
				}
			}
			if (insertionEndPos > newSectionStart) {
				this.Sections.Insert(pos++, new HighlightedSection {
					Offset = newSectionStart,
					Length = insertionEndPos - newSectionStart,
					Color = color
				});
				newSectionStart = insertionEndPos;
			}
		}
		#endregion

		/// <inheritdoc/>
		public override string ToString()
		{
			return "[HighlightedLine " + DocumentLine + " sections=" + Sections.Count + "]";
		}
	}
}
