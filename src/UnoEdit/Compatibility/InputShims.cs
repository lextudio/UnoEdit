// Portable shims for System.Windows.Input types.
// These allow AvalonEdit files that use RoutedCommand, KeyGesture, etc. to compile
// in UnoEdit without WPF.  The implementations are intentionally minimal — they
// satisfy the compiler and preserve command identity (Name), but do not replicate
// WPF's command-routing or gesture-matching behaviour.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace System.Windows.Input
{
	// -------------------------------------------------------------------------
	// Key enum — subset used by AvalonEdit commands and gestures.
	// Values match WPF's System.Windows.Input.Key for documentation clarity,
	// but UnoEdit never performs keyboard dispatch from this enum directly.
	// -------------------------------------------------------------------------
	public enum Key
	{
		None        = 0,
		Back        = 2,
		Tab         = 3,
		Return      = 6,
		Escape      = 27,
		Space       = 18,
		End         = 35,
		Home        = 36,
		Left        = 37,
		Up          = 38,
		Right       = 39,
		Down        = 40,
		Delete      = 46,
		Insert      = 45,
		F1  = 112, F2  = 113, F3  = 114, F4  = 115,
		F5  = 116, F6  = 117, F7  = 118, F8  = 119,
		F9  = 120, F10 = 121, F11 = 122, F12 = 123,
		// Letters (A–Z)
		A = 65, B = 66, C = 67, D = 68, E = 69, F = 70,
		G = 71, H = 72, I = 73, J = 74, K = 75, L = 76,
		M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82,
		S = 83, T = 84, U = 85, V = 86, W = 87, X = 88,
		Y = 89, Z = 90,
	}

	// -------------------------------------------------------------------------
	// ModifierKeys flags enum — mirrors WPF's ModifierKeys exactly.
	// -------------------------------------------------------------------------
	[Flags]
	public enum ModifierKeys
	{
		None    = 0,
		Alt     = 1,
		Control = 2,
		Shift   = 4,
		Windows = 8,
	}

	// -------------------------------------------------------------------------
	// InputGesture / KeyGesture
	// -------------------------------------------------------------------------
	public abstract class InputGesture { }

	public class KeyGesture : InputGesture
	{
		public Key            Key           { get; }
		public ModifierKeys   Modifiers     { get; }
		public string         DisplayString { get; }

		public KeyGesture(Key key)
			: this(key, ModifierKeys.None) { }

		public KeyGesture(Key key, ModifierKeys modifiers)
			: this(key, modifiers, string.Empty) { }

		public KeyGesture(Key key, ModifierKeys modifiers, string displayString)
		{
			Key           = key;
			Modifiers     = modifiers;
			DisplayString = displayString ?? string.Empty;
		}
	}

	/// <summary>Collection of <see cref="InputGesture"/> objects.</summary>
	public class InputGestureCollection : List<InputGesture>
	{
		public InputGestureCollection() { }
		public InputGestureCollection(IEnumerable<InputGesture> gestures) : base(gestures) { }
	}

	// -------------------------------------------------------------------------
	// ICommand — already provided by .NET, but RoutedCommand implements it.
	// -------------------------------------------------------------------------

	/// <summary>
	/// Compiler shim for <c>System.Windows.Input.RoutedCommand</c>.
	/// Preserves the command name for diagnostics; does not perform WPF routing.
	/// </summary>
	public class RoutedCommand : ICommand
	{
		public string Name { get; }

		public RoutedCommand(string name, Type ownerType)
		{
			Name = name ?? string.Empty;
		}

		public RoutedCommand(string name, Type ownerType, InputGestureCollection inputGestures)
		{
			Name = name ?? string.Empty;
		}

		public bool CanExecute(object parameter) => true;
		public void Execute(object parameter) { }
#pragma warning disable 67
		public event EventHandler CanExecuteChanged { add { } remove { } }
#pragma warning restore 67
	}

	// -------------------------------------------------------------------------
	// ApplicationCommands / EditingCommands
	// -------------------------------------------------------------------------
	public static class ApplicationCommands
	{
		public static RoutedCommand Copy      { get; } = new RoutedCommand("Copy",      typeof(ApplicationCommands));
		public static RoutedCommand Cut       { get; } = new RoutedCommand("Cut",       typeof(ApplicationCommands));
		public static RoutedCommand Paste     { get; } = new RoutedCommand("Paste",     typeof(ApplicationCommands));
		public static RoutedCommand Undo      { get; } = new RoutedCommand("Undo",      typeof(ApplicationCommands));
		public static RoutedCommand Redo      { get; } = new RoutedCommand("Redo",      typeof(ApplicationCommands));
		public static RoutedCommand SelectAll { get; } = new RoutedCommand("SelectAll", typeof(ApplicationCommands));
		public static RoutedCommand Delete    { get; } = new RoutedCommand("Delete",    typeof(ApplicationCommands));
		public static RoutedCommand Find      { get; } = new RoutedCommand("Find",      typeof(ApplicationCommands));
	}

	public static class EditingCommands
	{
		public static RoutedCommand Backspace             { get; } = new RoutedCommand("Backspace",             typeof(EditingCommands));
		public static RoutedCommand Delete                { get; } = new RoutedCommand("Delete",                typeof(EditingCommands));
		public static RoutedCommand DeleteNextWord        { get; } = new RoutedCommand("DeleteNextWord",        typeof(EditingCommands));
		public static RoutedCommand DeletePreviousWord    { get; } = new RoutedCommand("DeletePreviousWord",    typeof(EditingCommands));
		public static RoutedCommand EnterParagraphBreak   { get; } = new RoutedCommand("EnterParagraphBreak",   typeof(EditingCommands));
		public static RoutedCommand EnterLineBreak        { get; } = new RoutedCommand("EnterLineBreak",        typeof(EditingCommands));
		public static RoutedCommand TabForward            { get; } = new RoutedCommand("TabForward",            typeof(EditingCommands));
		public static RoutedCommand TabBackward           { get; } = new RoutedCommand("TabBackward",           typeof(EditingCommands));
		public static RoutedCommand MoveToLineStart       { get; } = new RoutedCommand("MoveToLineStart",       typeof(EditingCommands));
		public static RoutedCommand MoveToLineEnd         { get; } = new RoutedCommand("MoveToLineEnd",         typeof(EditingCommands));
		public static RoutedCommand SelectToLineStart     { get; } = new RoutedCommand("SelectToLineStart",     typeof(EditingCommands));
		public static RoutedCommand SelectToLineEnd       { get; } = new RoutedCommand("SelectToLineEnd",       typeof(EditingCommands));
		public static RoutedCommand MoveToDocumentStart   { get; } = new RoutedCommand("MoveToDocumentStart",   typeof(EditingCommands));
		public static RoutedCommand MoveToDocumentEnd     { get; } = new RoutedCommand("MoveToDocumentEnd",     typeof(EditingCommands));
		public static RoutedCommand SelectToDocumentStart { get; } = new RoutedCommand("SelectToDocumentStart", typeof(EditingCommands));
		public static RoutedCommand SelectToDocumentEnd   { get; } = new RoutedCommand("SelectToDocumentEnd",   typeof(EditingCommands));
		public static RoutedCommand MoveLeftByWord        { get; } = new RoutedCommand("MoveLeftByWord",        typeof(EditingCommands));
		public static RoutedCommand MoveRightByWord       { get; } = new RoutedCommand("MoveRightByWord",       typeof(EditingCommands));
		public static RoutedCommand SelectLeftByWord      { get; } = new RoutedCommand("SelectLeftByWord",      typeof(EditingCommands));
		public static RoutedCommand SelectRightByWord     { get; } = new RoutedCommand("SelectRightByWord",     typeof(EditingCommands));
	}

	// -------------------------------------------------------------------------
	// CommandBinding / CommandBindingCollection
	// -------------------------------------------------------------------------
	public class ExecutedRoutedEventArgs : System.Windows.RoutedEventArgs
	{
		public ICommand Command { get; }
		public object   Parameter { get; }
		public new bool Handled { get; set; }

		public ExecutedRoutedEventArgs(ICommand command, object parameter)
		{
			Command   = command;
			Parameter = parameter;
		}
	}

	public class CanExecuteRoutedEventArgs : System.Windows.RoutedEventArgs
	{
		public ICommand Command       { get; }
		public object   Parameter     { get; }
		public bool     CanExecute    { get; set; } = true;
		public new bool Handled       { get; set; }
		public bool     ContinueRouting { get; set; }

		public CanExecuteRoutedEventArgs(ICommand command, object parameter)
		{
			Command   = command;
			Parameter = parameter;
		}
	}

	public class CommandBinding
	{
		public ICommand Command { get; }

		public CommandBinding(ICommand command) { Command = command; }

		public CommandBinding(
			ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed)
			: this(command) { }

		public CommandBinding(
			ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed,
			EventHandler<CanExecuteRoutedEventArgs> canExecute)
			: this(command) { }
	}

	public class CommandBindingCollection : List<CommandBinding>
	{
		public void Add(ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed) =>
			Add(new CommandBinding(command, executed));

		public void Add(ICommand command,
			EventHandler<ExecutedRoutedEventArgs> executed,
			EventHandler<CanExecuteRoutedEventArgs> canExecute) =>
			Add(new CommandBinding(command, executed, canExecute));
	}
}

// RoutedEvent and RoutedEventArgs live in System.Windows namespace.
namespace System.Windows
{
	public sealed class RoutedEvent
	{
		public string Name { get; }
		public RoutedEvent(string name) { Name = name ?? string.Empty; }
	}

	public class RoutedEventArgs : EventArgs
	{
		public RoutedEvent RoutedEvent { get; set; }
		public bool        Handled     { get; set; }
		public object      Source      { get; set; }
	}
}
