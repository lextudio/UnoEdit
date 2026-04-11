// Stubs for input handler types: ITextAreaInputHandler, TextAreaInputHandler,
// TextAreaStackedInputHandler, TextAreaDefaultInputHandler.
// Uses object for TextArea parameter to avoid cross-project dependency.
using System;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Interface to be implemented by input handlers for the TextArea.
	/// </summary>
	public interface ITextAreaInputHandler
	{
		/// <summary>Gets the text area this handler belongs to.</summary>
		object TextArea { get; }

		/// <summary>Attaches the handler to the text area.</summary>
		void Attach();

		/// <summary>Detaches the handler from the text area.</summary>
		void Detach();
	}

	/// <summary>
	/// Abstract stacked input handler (processes events in order, without replacing active handler).
	/// </summary>
	public abstract class TextAreaStackedInputHandler : ITextAreaInputHandler
	{
		/// <inheritdoc/>
		public object TextArea { get; }

		/// <summary>Creates a new TextAreaStackedInputHandler.</summary>
		protected TextAreaStackedInputHandler(object textArea) { TextArea = textArea; }

		/// <inheritdoc/>
		public virtual void Attach() { }

		/// <inheritdoc/>
		public virtual void Detach() { }

		/// <summary>Called for PreviewKeyDown events.</summary>
		public virtual void OnPreviewKeyDown(object e) { }

		/// <summary>Called for PreviewKeyUp events.</summary>
		public virtual void OnPreviewKeyUp(object e) { }
	}

	/// <summary>
	/// Default implementation of ITextAreaInputHandler with command/input binding collections.
	/// </summary>
	public class TextAreaInputHandler : ITextAreaInputHandler
	{
		public sealed class CommandBindingDescriptor
		{
			public CommandBindingDescriptor(object command, object handler)
			{
				Command = command;
				Handler = handler;
			}

			public object Command { get; }
			public object Handler { get; }
		}

		public sealed class InputBindingDescriptor
		{
			public InputBindingDescriptor(object command, object modifiers, object key)
			{
				Command = command;
				Modifiers = modifiers;
				Key = key;
			}

			public object Command { get; }
			public object Modifiers { get; }
			public object Key { get; }
		}

		/// <inheritdoc/>
		public object TextArea { get; }

		/// <summary>Creates a new TextAreaInputHandler.</summary>
		public TextAreaInputHandler(object textArea) { TextArea = textArea; }

		/// <summary>Gets whether the handler is attached to the text area.</summary>
		public bool IsAttached { get; private set; }

		/// <summary>Gets the command bindings.</summary>
		public ICollection<object> CommandBindings { get; } = new List<object>();

		/// <summary>Gets the input bindings.</summary>
		public ICollection<object> InputBindings { get; } = new List<object>();

		/// <summary>Gets the nested input handlers.</summary>
		public ICollection<ITextAreaInputHandler> NestedInputHandlers { get; } = new List<ITextAreaInputHandler>();

		/// <summary>Adds a command and keyboard shortcut binding.</summary>
		public void AddBinding(object command, object modifiers, object key, object handler)
		{
			CommandBindings.Add(new CommandBindingDescriptor(command, handler));
			InputBindings.Add(new InputBindingDescriptor(command, modifiers, key));
		}

		/// <inheritdoc/>
		public virtual void Attach()
		{
			if (IsAttached)
				return;

			IsAttached = true;
			foreach (var handler in NestedInputHandlers)
			{
				handler?.Attach();
			}
		}

		/// <inheritdoc/>
		public virtual void Detach()
		{
			if (!IsAttached)
				return;

			foreach (var handler in NestedInputHandlers)
			{
				handler?.Detach();
			}

			IsAttached = false;
		}
	}

	/// <summary>
	/// Default input handler including caret navigation, editing, and mouse selection handlers.
	/// </summary>
	public class TextAreaDefaultInputHandler : TextAreaInputHandler
	{
		/// <summary>Creates a new TextAreaDefaultInputHandler.</summary>
		public TextAreaDefaultInputHandler(object textArea) : base(textArea)
		{
			CaretNavigation = new TextAreaInputHandler(textArea);
			Editing = new TextAreaInputHandler(textArea);
			MouseSelection = new TextAreaInputHandler(textArea);

			NestedInputHandlers.Add(CaretNavigation);
			NestedInputHandlers.Add(Editing);
			NestedInputHandlers.Add(MouseSelection);
		}

		/// <summary>Gets the caret navigation input handler.</summary>
		public TextAreaInputHandler CaretNavigation { get; }

		/// <summary>Gets the editing input handler.</summary>
		public TextAreaInputHandler Editing { get; }

		/// <summary>Gets the mouse selection input handler.</summary>
		public ITextAreaInputHandler MouseSelection { get; }
	}
}
