// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF FlowDocument removed.

using System;
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
		public static RichText ConvertTextDocumentToRichText(IDocument document, IHighlighter highlighter) => RichText.Empty;

		/// <summary>Creates a FlowDocument for a TextEditor (stub — returns null).</summary>
		public static object CreateFlowDocumentForEditor(object editor) => null;
	}
}
