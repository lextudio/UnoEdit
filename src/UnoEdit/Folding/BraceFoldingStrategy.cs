// Copyright (c) 2009 Daniel Grunwald (original AvalonEdit sample)
// Ported to UnoEdit portable core.

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// Produces foldings based on matching brace characters.
	/// </summary>
	public class BraceFoldingStrategy
	{
		/// <summary>Gets/sets the opening brace character.  Default: <c>'{'</c>.</summary>
		public char OpeningBrace { get; set; } = '{';

		/// <summary>Gets/sets the closing brace character.  Default: <c>'}'</c>.</summary>
		public char ClosingBrace { get; set; } = '}';

		/// <summary>Updates <paramref name="manager"/> with foldings derived from the current document.</summary>
		public void UpdateFoldings(FoldingManager manager, TextDocument document)
		{
			if (manager == null) throw new ArgumentNullException("manager");
			if (document == null) throw new ArgumentNullException("document");
			int firstErrorOffset;
			manager.UpdateFoldings(CreateNewFoldings(document, out firstErrorOffset), firstErrorOffset);
		}

		/// <summary>Creates <see cref="NewFolding"/> instances for <paramref name="document"/>.</summary>
		public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
		{
			firstErrorOffset = -1;
			return CreateNewFoldings(document);
		}

		/// <summary>Creates <see cref="NewFolding"/> instances for <paramref name="document"/>.</summary>
		public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
		{
			var newFoldings = new List<NewFolding>();
			var startOffsets = new Stack<int>();
			int lastNewLineOffset = 0;
			char opening = OpeningBrace;
			char closing = ClosingBrace;
			for (int i = 0; i < document.TextLength; i++) {
				char c = document.GetCharAt(i);
				if (c == opening) {
					startOffsets.Push(i);
				} else if (c == closing && startOffsets.Count > 0) {
					int startOffset = startOffsets.Pop();
					// Don't fold if opening and closing brace are on the same line.
					if (startOffset < lastNewLineOffset) {
						newFoldings.Add(new NewFolding(startOffset, i + 1));
					}
				} else if (c == '\n' || c == '\r') {
					lastNewLineOffset = i + 1;
				}
			}
			newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}
	}
}
