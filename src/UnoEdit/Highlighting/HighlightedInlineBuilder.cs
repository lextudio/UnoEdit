// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Builds highlighted inline runs from rich text.
	/// Not used in AvalonEdit rendering itself but useful for external consumers.
	/// </summary>
	public sealed class HighlightedInlineBuilder
	{
		/// <summary>Creates a new HighlightedInlineBuilder for the given text.</summary>
		public HighlightedInlineBuilder(string text) { Text = text; }

		/// <summary>Creates a new HighlightedInlineBuilder from a RichText.</summary>
		public HighlightedInlineBuilder(RichText text) { Text = text?.Text ?? string.Empty; }

		/// <summary>Gets the text.</summary>
		public string Text { get; }

		/// <summary>Applies a highlighting color to a range.</summary>
		public void SetHighlighting(int offset, int length, HighlightingColor color) { }

		/// <summary>Sets the foreground brush for a range.</summary>
		public void SetForeground(int offset, int length, object brush) { }

		/// <summary>Sets the background brush for a range.</summary>
		public void SetBackground(int offset, int length, object brush) { }

		/// <summary>Sets the font weight for a range.</summary>
		public void SetFontWeight(int offset, int length, object weight) { }

		/// <summary>Sets the font style for a range.</summary>
		public void SetFontStyle(int offset, int length, object style) { }

		/// <summary>Creates inline run objects.</summary>
		public object[] CreateRuns() => Array.Empty<object>();

		/// <summary>Converts to a RichText.</summary>
		public RichText ToRichText() => RichText.Empty;

		/// <summary>Creates a clone of this builder.</summary>
		public HighlightedInlineBuilder Clone() => new HighlightedInlineBuilder(Text);
	}
}
