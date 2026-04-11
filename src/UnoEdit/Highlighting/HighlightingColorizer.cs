// UnoEdit stub for HighlightingColorizer.
// The full WPF implementation uses VisualLineElementTextRunProperties.SetForegroundBrush,
// SetTypeface, SetTextDecorations etc. which are not yet available in this port.

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Stub implementation of HighlightingColorizer.
	/// Inherits from DocumentColorizingTransformer to satisfy the type hierarchy.
	/// Full rendering support requires VisualLineElementTextRunProperties WPF members.
	/// </summary>
	public class HighlightingColorizer : Rendering.DocumentColorizingTransformer
	{
		/// <summary>Creates a colorizer using the specified highlighting definition.</summary>
		public HighlightingColorizer(IHighlightingDefinition definition) { }

		/// <summary>Creates a colorizer using a fixed highlighter.</summary>
		public HighlightingColorizer(IHighlighter highlighter) { }

		/// <summary>Creates a colorizer for subclassing.</summary>
		protected HighlightingColorizer() { }

		/// <inheritdoc/>
		protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line) { }
	}
}
