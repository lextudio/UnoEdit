// UnoEdit stub for ICompletionData.
// The TextArea parameter uses object to avoid a cross-project dependency on
// ICSharpCode.AvalonEdit.Editing.TextArea (which lives in the Sample project).

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Describes an entry in the code-completion list.
	/// </summary>
	public interface ICompletionData
	{
		/// <summary>Gets the image displayed next to the entry.</summary>
		ImageSource? Image { get; }

		/// <summary>Gets the text used for filtering and insertion.</summary>
		string Text { get; }

		/// <summary>Gets the displayed content (string or UIElement).</summary>
		object Content { get; }

		/// <summary>Gets the description shown in the tooltip.</summary>
		object Description { get; }

		/// <summary>Gets the priority used for sorting and auto-selection.</summary>
		double Priority { get; }

		/// <summary>
		/// Called to perform the completion.
		/// </summary>
		/// <param name="textArea">The text area (passed as object to avoid cross-project dependency).</param>
		/// <param name="completionSegment">The segment that was replaced by the completion.</param>
		/// <param name="insertionRequestEventArgs">Event args that triggered the insertion.</param>
		void Complete(object textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs);
	}
}
