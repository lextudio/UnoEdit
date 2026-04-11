// Uno-native InsightWindow and OverloadInsightWindow.
// InsightWindow is a popup-based window for showing parameter/type information
// (e.g. function signature hints). OverloadInsightWindow wraps an OverloadViewer
// to display multiple overloads with Up/Down navigation.
using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// A popup-like window attached to a text segment that shows insight information
	/// (e.g. method signature hints).
	/// </summary>
	public class InsightWindow : CompletionWindowBase
	{
		/// <summary>
		/// Gets/Sets whether the insight window closes automatically.
		/// The default is <c>true</c>.
		/// </summary>
		public bool CloseAutomatically { get; set; } = true;

		/// <summary>Creates a new InsightWindow for <paramref name="textArea"/>.</summary>
		public InsightWindow(FrameworkElement textArea) : base(textArea) { }

		/// <summary>Shows the insight window with the given <paramref name="content"/>.</summary>
		public void Show(UIElement content)
		{
			popup.Child = content;
			base.Show();
		}
	}

	/// <summary>
	/// Insight window that displays an <see cref="OverloadViewer"/> allowing the user
	/// to cycle through multiple overloads.
	/// </summary>
	public class OverloadInsightWindow : InsightWindow
	{
		readonly OverloadViewer overloadViewer;

		/// <summary>Creates a new OverloadInsightWindow for <paramref name="textArea"/>.</summary>
		public OverloadInsightWindow(FrameworkElement textArea) : base(textArea)
		{
			overloadViewer = new OverloadViewer();
			popup.Child = overloadViewer;
		}

		/// <summary>Gets/Sets the overload provider shown in the window.</summary>
		public IOverloadProvider Provider
		{
			get => overloadViewer.Provider;
			set => overloadViewer.Provider = value;
		}

		/// <summary>Shows the overload insight window.</summary>
		public new void Show() => base.Show(overloadViewer);
	}
}
