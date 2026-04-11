// UnoEdit port of DottedLineMargin.
// Uses Microsoft.UI.Xaml.Shapes.Line (WinUI/Uno) instead of WPF System.Windows.Shapes.Line.
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Margin for use with the text area.
	/// A vertical dotted line to separate the line numbers from the text view.
	/// </summary>
	public static class DottedLineMargin
	{
		static readonly object tag = new object();

		/// <summary>
		/// Creates a vertical dotted line to separate the line numbers from the text view.
		/// </summary>
		public static UIElement Create()
		{
			var line = new Line {
				X1 = 0,
				Y1 = 0,
				X2 = 0,
				Y2 = 1,
				StrokeDashArray = new DoubleCollection { 0, 2 },
				Stretch = Stretch.Fill,
				StrokeThickness = 1,
				Margin = new Microsoft.UI.Xaml.Thickness(2, 0, 2, 0),
				Tag = tag
			};
			return line;
		}

		/// <summary>
		/// Gets whether the specified UIElement is the result of a <see cref="Create"/> call.
		/// </summary>
		public static bool IsDottedLineMargin(UIElement element)
		{
			return element is Line l && l.Tag == tag;
		}
	}
}
