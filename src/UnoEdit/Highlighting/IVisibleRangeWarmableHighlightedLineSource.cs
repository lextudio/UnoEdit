namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Optional highlighter capability that allows the host to warm the current
	/// visible line range before the first repaint after attaching the source.
	/// </summary>
	public interface IVisibleRangeWarmableHighlightedLineSource
	{
		void WarmVisibleLineRange(int startLineNumber, int endLineNumber);
	}
}
