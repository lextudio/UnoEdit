using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Colorizer that applies a RichTextModel to the document.
	/// </summary>
	public class RichTextColorizer : DocumentColorizingTransformer
	{
		/// <summary>Creates a new RichTextColorizer.</summary>
		public RichTextColorizer(RichTextModel richTextModel) { }

		/// <inheritdoc/>
		protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line) { }
	}
}
