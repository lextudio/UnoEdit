// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF FlowDocument removed.

using System;
using System.Collections.Generic;
using System.Reflection;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
		/// Converts a TextDocument to rich text formats.
		/// Uses portable rich-text descriptors instead of WPF FlowDocument.
		/// </summary>
	public static class DocumentPrinter
	{
		public sealed class PrintedLine
		{
			public PrintedLine(int lineNumber, string text, RichText richText)
			{
				LineNumber = lineNumber;
				Text = text ?? string.Empty;
				RichText = richText ?? new RichText(string.Empty);
			}

			public int LineNumber { get; }
			public string Text { get; }
			public RichText RichText { get; }
		}

		public sealed class PrintedDocument
		{
			public PrintedDocument(IReadOnlyList<PrintedLine> lines, RichText fullText)
			{
				Lines = lines ?? Array.Empty<PrintedLine>();
				FullText = fullText ?? new RichText(string.Empty);
			}

			public IReadOnlyList<PrintedLine> Lines { get; }
			public RichText FullText { get; }
		}

		/// <summary>Converts a document to a portable printable descriptor object.</summary>
		public static object ConvertTextDocumentToBlock(IDocument document, IHighlighter highlighter)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			var lines = new List<PrintedLine>(Math.Max(1, document.LineCount));
			for (int lineNumber = 1; lineNumber <= document.LineCount; lineNumber++)
			{
				IDocumentLine line = document.GetLineByNumber(lineNumber);
				string text = document.GetText(line);
				RichText richText = highlighter != null
					? highlighter.HighlightLine(lineNumber).ToRichText()
					: new RichText(text);
				lines.Add(new PrintedLine(lineNumber, text, richText));
			}

			return new PrintedDocument(lines, ConvertTextDocumentToRichText(document, highlighter));
		}

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

		/// <summary>Creates a portable printable descriptor object from a TextEditor-like object.</summary>
		public static object CreateFlowDocumentForEditor(object editor)
		{
			if (editor == null)
				throw new ArgumentNullException(nameof(editor));

			var editorType = editor.GetType();
			IDocument document = editorType.GetProperty("Document", BindingFlags.Public | BindingFlags.Instance)?.GetValue(editor) as IDocument;
			if (document == null)
				throw new ArgumentException("editor must expose a Document property implementing IDocument", nameof(editor));

			// Keep this portable: if no explicit highlighter service exists we still return plain text output.
			IHighlighter highlighter = editorType.GetProperty("Highlighter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(editor) as IHighlighter;

			return ConvertTextDocumentToBlock(document, highlighter);
		}
	}
}
