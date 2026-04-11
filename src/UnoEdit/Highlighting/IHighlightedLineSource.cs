using System;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Provides highlighted lines for the Uno text surface.
	/// This keeps the current Avalon/XSHD highlighter path and optional external highlighters
	/// on the same seam.
	/// </summary>
	public interface IHighlightedLineSource : IDisposable
	{
		/// <summary>
		/// Raised when highlighting state changes and the host should redraw affected lines.
		/// </summary>
		event EventHandler HighlightingInvalidated;

		/// <summary>
		/// Attaches the source to a document. The source may dispose and rebuild internal state.
		/// </summary>
		void SetDocument(TextDocument? document);

		/// <summary>
		/// Gets the highlighted line for a 1-based line number.
		/// </summary>
		HighlightedLine HighlightLine(int lineNumber);
	}
}
