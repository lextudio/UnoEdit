// Forked from AvalonEdit for UnoEdit — command definitions kept, owner type decoupled from WPF TextEditor.
// Original: ICSharpCode.AvalonEdit/AvalonEditCommands.cs

using System.Windows.Input;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Custom commands for UnoEdit.
	/// </summary>
	public static class AvalonEditCommands
	{
		public static readonly RoutedCommand ToggleOverstrike = new RoutedCommand(
			"ToggleOverstrike", typeof(AvalonEditCommands),
			new InputGestureCollection {
				new KeyGesture(Key.Insert)
			});

		public static readonly RoutedCommand DeleteLine = new RoutedCommand(
			"DeleteLine", typeof(AvalonEditCommands),
			new InputGestureCollection {
				new KeyGesture(Key.D, ModifierKeys.Control)
			});

		public static readonly RoutedCommand RemoveLeadingWhitespace = new RoutedCommand("RemoveLeadingWhitespace", typeof(AvalonEditCommands));
		public static readonly RoutedCommand RemoveTrailingWhitespace = new RoutedCommand("RemoveTrailingWhitespace", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertToUppercase = new RoutedCommand("ConvertToUppercase", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertToLowercase = new RoutedCommand("ConvertToLowercase", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertToTitleCase = new RoutedCommand("ConvertToTitleCase", typeof(AvalonEditCommands));
		public static readonly RoutedCommand InvertCase = new RoutedCommand("InvertCase", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertTabsToSpaces = new RoutedCommand("ConvertTabsToSpaces", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertSpacesToTabs = new RoutedCommand("ConvertSpacesToTabs", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertLeadingTabsToSpaces = new RoutedCommand("ConvertLeadingTabsToSpaces", typeof(AvalonEditCommands));
		public static readonly RoutedCommand ConvertLeadingSpacesToTabs = new RoutedCommand("ConvertLeadingSpacesToTabs", typeof(AvalonEditCommands));

		public static readonly RoutedCommand IndentSelection = new RoutedCommand(
			"IndentSelection", typeof(AvalonEditCommands),
			new InputGestureCollection {
				new KeyGesture(Key.I, ModifierKeys.Control)
			});
	}
}
