// Forked from AvalonEdit for UnoEdit — command definitions kept, SearchInputHandler omitted.
// Original: ICSharpCode.AvalonEdit/Search/SearchCommands.cs

using System.Windows.Input;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Search commands for UnoEdit.
	/// </summary>
	public static class SearchCommands
	{
		/// <summary>
		/// Finds the next occurrence in the file.
		/// </summary>
		public static readonly RoutedCommand FindNext = new RoutedCommand(
			"FindNext", typeof(SearchCommands),
			new InputGestureCollection { new KeyGesture(Key.F3) }
		);

		/// <summary>
		/// Finds the previous occurrence in the file.
		/// </summary>
		public static readonly RoutedCommand FindPrevious = new RoutedCommand(
			"FindPrevious", typeof(SearchCommands),
			new InputGestureCollection { new KeyGesture(Key.F3, ModifierKeys.Shift) }
		);

		/// <summary>
		/// Closes the search surface.
		/// </summary>
		public static readonly RoutedCommand CloseSearchPanel = new RoutedCommand(
			"CloseSearchPanel", typeof(SearchCommands),
			new InputGestureCollection { new KeyGesture(Key.Escape) }
		);
	}
}
