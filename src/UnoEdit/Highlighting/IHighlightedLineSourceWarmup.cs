namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Optional highlighter capability that allows eager tokenization of a line range
	/// before the host renders it.
	/// </summary>
	public interface IHighlightedLineSourceWarmup
	{
		/// <summary>
		/// Prepares highlighting state for the inclusive 1-based line range.
		/// </summary>
		void WarmupLineRange(int startLineNumber, int endLineNumber);
	}
}
