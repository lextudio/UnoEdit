using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Optional highlighter capability that allows the source to observe the hosting UI text view.
	/// TextMate uses this to follow viewport changes similarly to AvaloniaEdit.
	/// </summary>
	public interface ITextViewAwareHighlightedLineSource
	{
		void SetTextView(ITextView? textView);
	}
}
