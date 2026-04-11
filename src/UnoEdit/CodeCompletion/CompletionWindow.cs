using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// The code completion window. Shows a <see cref="CompletionList"/> inside a popup
	/// positioned from the same caret-rectangle path used by the platform IME bridge.
	/// </summary>
	public class CompletionWindow : CompletionWindowBase
	{
		readonly CompletionList completionList;

		/// <summary>Gets the <see cref="CompletionList"/> shown in this window.</summary>
		public CompletionList CompletionList => completionList;

		/// <summary>
		/// Gets/Sets whether the window closes automatically as the user types.
		/// The default is <c>true</c>.
		/// </summary>
		public bool CloseAutomatically { get; set; } = true;

		/// <summary>
		/// Gets/Sets whether the window closes when the caret moves to before
		/// <see cref="CompletionWindowBase.StartOffset"/>.
		/// </summary>
		public bool CloseWhenCaretAtBeginning { get; set; }

		/// <summary>
		/// Creates a new code completion window attached to <paramref name="textArea"/>.
		/// </summary>
		public CompletionWindow(FrameworkElement textArea) : base(textArea)
		{
			completionList = new CompletionList();
			completionList.InsertionRequested += (_, e) => Close();

			completionList.Width = 175;
			completionList.MaxHeight = 300;
			completionList.MinHeight = 15;
			completionList.MinWidth = 30;

			popup.Child = completionList;
		}

		/// <summary>Shows the completion window.</summary>
		public new void Show() => base.Show();
	}
}
