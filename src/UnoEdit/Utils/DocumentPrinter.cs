// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF FlowDocument removed.

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Converts a TextDocument to rich text formats.
	/// Stub — WPF FlowDocument types not available in Uno.
	/// </summary>
	public static class DocumentPrinter
	{
		/// <summary>Converts a document to a Block (stub — returns null).</summary>
		public static object ConvertTextDocumentToBlock(IDocument document, IHighlighter highlighter) => null;

		/// <summary>Converts a document to RichText.</summary>
		public static RichText ConvertTextDocumentToRichText(IDocument document, IHighlighter highlighter)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			var parts = new List<RichText>();
			for (var lineNumber = 1; lineNumber <= document.LineCount; lineNumber++) {
				var line = document.GetLineByNumber(lineNumber);
				if (lineNumber > 1)
					parts.Add(new RichText(line.PreviousLine.DelimiterLength == 2 ? "\r\n" : "\n"));

				if (highlighter != null)
					parts.Add(highlighter.HighlightLine(lineNumber).ToRichText());
				else
					parts.Add(new RichText(document.GetText(line)));
			}

			return RichText.Concat(parts.ToArray());
		}

		/// <summary>Creates a FlowDocument for a TextEditor (stub — returns null).</summary>
		public static object CreateFlowDocumentForEditor(object editor) => null;
	}
}
