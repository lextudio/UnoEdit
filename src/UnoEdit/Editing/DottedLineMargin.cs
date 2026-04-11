// UnoEdit stub for DottedLineMargin.
// The WPF Shapes.Line is not available; the visual is rendered via Uno XAML.

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Margin for use with the text area.
	/// A vertical dotted line to separate the line numbers from the text view.
	/// </summary>
	public static class DottedLineMargin
	{
		/// <summary>
		/// Creates a vertical dotted line to separate the line numbers from the text view.
		/// Returns null in this Uno stub; the visual is rendered natively.
		/// </summary>
		public static UIElement Create() => null;

		/// <summary>
		/// Gets whether the specified UIElement is the result of a <see cref="Create"/> call.
		/// Always returns false in this Uno stub.
		/// </summary>
		public static bool IsDottedLineMargin(UIElement element) => false;
	}
}
