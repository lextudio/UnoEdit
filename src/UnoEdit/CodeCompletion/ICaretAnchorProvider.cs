using Windows.Foundation;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Provides the current caret anchor rectangle in XamlRoot coordinates.
	/// Completion and insight popups use this rectangle so they follow the
	/// same caret placement path as the platform IME integration.
	/// </summary>
	public interface ICaretAnchorProvider
	{
		/// <summary>
		/// Tries to return the current caret anchor rectangle in XamlRoot coordinates.
		/// </summary>
		bool TryGetCaretAnchorRect(out Rect rect);
	}
}
