// Uno-native implementation of the code completion popup window.
// Uses CompletionWindowBase (Popup) + CompletionList (ListView) to display
// completion items and insert the selected entry on confirmation.
using System;
using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// The code completion window. Shows a <see cref="CompletionList"/> inside a popup
	/// positioned near the text caret.
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

			// Size the popup content.
			completionList.Width = 175;
			completionList.MaxHeight = 300;
			completionList.MinHeight = 15;
			completionList.MinWidth = 30;

			popup.Child = completionList;
		}

		/// <summary>Shows the completion window.</summary>
		public void Show() => base.Show();

		/// <inheritdoc/>
		public override void Close()
		{
			if (popup.IsOpen)
				base.Close();
		}
	}
}
