// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Provides HTML clipboard format for highlighted text.
	/// </summary>
	public static class HtmlClipboard
	{
		/// <summary>Sets the HTML content on a DataObject.</summary>
		public static void SetHtml(object dataObject, string htmlFragment) { }

		/// <summary>Creates an HTML fragment for the specified document range.</summary>
		public static string CreateHtmlFragment(IDocument document, IHighlighter highlighter, ISegment segment, HtmlOptions options)
			=> string.Empty;
	}
}
