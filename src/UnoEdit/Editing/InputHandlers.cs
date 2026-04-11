// Stubs for input handler types: ITextAreaInputHandler, TextAreaInputHandler,
// TextAreaStackedInputHandler, TextAreaDefaultInputHandler.
// Uses object for TextArea parameter to avoid cross-project dependency.
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
		public void AddBinding(object command, object modifiers, object key, object handler) { }

		/// <inheritdoc/>
		public virtual void Attach() { IsAttached = true; }

		/// <inheritdoc/>
		public virtual void Detach() { IsAttached = false; }
	}

	/// <summary>
	/// Default input handler including caret navigation, editing, and mouse selection handlers.
	/// </summary>
	public class TextAreaDefaultInputHandler : TextAreaInputHandler
	{
		/// <summary>Creates a new TextAreaDefaultInputHandler.</summary>
		public TextAreaDefaultInputHandler(object textArea) : base(textArea) { }

		/// <summary>Gets the caret navigation input handler.</summary>
		public TextAreaInputHandler CaretNavigation { get; } = null;

		/// <summary>Gets the editing input handler.</summary>
		public TextAreaInputHandler Editing { get; } = null;

		/// <summary>Gets the mouse selection input handler.</summary>
		public ITextAreaInputHandler MouseSelection { get; } = null;
	}
}
