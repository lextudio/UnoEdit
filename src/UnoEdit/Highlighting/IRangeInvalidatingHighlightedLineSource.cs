using System;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Optional highlighter capability that reports exact document line ranges whose
	/// highlighting changed, allowing the host to repaint incrementally.
	/// </summary>
	public interface IRangeInvalidatingHighlightedLineSource
	{
		event EventHandler<HighlightedLineRangeInvalidatedEventArgs>? HighlightingRangeInvalidated;
	}
}
