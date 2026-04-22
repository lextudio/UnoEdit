namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Optional highlighter capability that reports whether a visible document line range
	/// has fully materialized highlight data and can be painted without placeholder/null results.
	/// </summary>
	public interface IVisibleRangeReadyHighlightedLineSource
	{
		bool IsVisibleLineRangeReady(int startLineNumber, int endLineNumber);
	}
}
