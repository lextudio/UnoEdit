// Linked directly from AvalonEdit — no WPF dependencies.
using System.ComponentModel;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Provides items for the OverloadViewer.
	/// </summary>
	public interface IOverloadProvider : INotifyPropertyChanged
	{
		/// <summary>Gets/Sets the selected index.</summary>
		int SelectedIndex { get; set; }

		/// <summary>Gets the number of overloads.</summary>
		int Count { get; }

		/// <summary>Gets the text 'x of y'.</summary>
		string CurrentIndexText { get; }

		/// <summary>Gets the header for the currently selected overload.</summary>
		object CurrentHeader { get; }

		/// <summary>Gets the content for the currently selected overload.</summary>
		object CurrentContent { get; }
	}
}
