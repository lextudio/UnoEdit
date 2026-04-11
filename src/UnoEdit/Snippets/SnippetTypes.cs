// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>Abstract base for snippet elements.</summary>
	[Serializable]
	public abstract class SnippetElement
	{
		/// <summary>Performs insertion of the snippet.</summary>
		public abstract void Insert(InsertionContext context);

		/// <summary>Converts the snippet to a text run placeholder.</summary>
		public virtual object ToTextRun() => null;
	}

	/// <summary>Inserts plain text.</summary>
	[Serializable]
	public class SnippetTextElement : SnippetElement
	{
		/// <summary>Gets/sets the text to insert.</summary>
		public string Text { get; set; }

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>Marks a caret position.</summary>
	[Serializable]
	public class SnippetCaretElement : SnippetElement
	{
		/// <summary>Creates a new SnippetCaretElement.</summary>
		public SnippetCaretElement() { }

		/// <summary>Creates a new SnippetCaretElement with the specified option.</summary>
		public SnippetCaretElement(bool setCaretOnlyIfTextIsSelected) { }

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }
	}

	/// <summary>Container element that groups child snippet elements.</summary>
	[Serializable]
	public class SnippetContainerElement : SnippetElement
	{
		/// <summary>Gets the child elements.</summary>
		public IList<SnippetElement> Elements { get; } = new List<SnippetElement>();

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>Marks an anchor point that can be referenced by bound elements.</summary>
	[Serializable]
	public sealed class SnippetAnchorElement : SnippetElement
	{
		/// <summary>Gets the name of this anchor.</summary>
		public string Name { get; private set; }

		/// <summary>Creates a new SnippetAnchorElement.</summary>
		public SnippetAnchorElement(string name) { Name = name; }

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }
	}

	/// <summary>Bound snippet element that mirrors a replaceable text element.</summary>
	[Serializable]
	public class SnippetBoundElement : SnippetElement
	{
		/// <summary>Gets/sets the target replaceable element.</summary>
		public SnippetReplaceableTextElement TargetElement { get; set; }

		/// <summary>Converts the text of the target element.</summary>
		public virtual string ConvertText(string input) => input;

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>Selects the current selection and indents it.</summary>
	[Serializable]
	public class SnippetSelectionElement : SnippetElement
	{
		/// <summary>Gets/sets the indentation level.</summary>
		public int Indentation { get; set; }

		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }
	}

	/// <summary>A replaceable text element in a snippet.</summary>
	[Serializable]
	public class SnippetReplaceableTextElement : SnippetTextElement
	{
		/// <inheritdoc/>
		public override void Insert(InsertionContext context) { }

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>A code snippet that can be inserted into the text editor.</summary>
	[Serializable]
	public class Snippet : SnippetContainerElement
	{
		/// <summary>Inserts the snippet into the text area.</summary>
		public void Insert(object textArea) { }
	}

	/// <summary>Active element registered by <see cref="SnippetAnchorElement"/>.</summary>
	public sealed class AnchorElement : IActiveElement
	{
		/// <summary>Creates a new AnchorElement.</summary>
		public AnchorElement(object segment, string name, InsertionContext context)
		{
			Name = name;
		}

		/// <inheritdoc/>
		public bool IsEditable => false;

		/// <inheritdoc/>
		public ISegment Segment => null;

		/// <summary>Gets the name of this anchor.</summary>
		public string Name { get; private set; }

		/// <summary>Gets the text at this anchor.</summary>
		public string Text => string.Empty;

		/// <inheritdoc/>
		public void OnInsertionCompleted() { }

		/// <inheritdoc/>
		public void Deactivate(SnippetEventArgs e) { }
	}

	/// <summary>Interface for replaceable active elements.</summary>
	public interface IReplaceableActiveElement : IActiveElement
	{
		/// <summary>Gets the current text.</summary>
		string Text { get; }

		/// <summary>Raised when Text changes.</summary>
		event EventHandler TextChanged;
	}
}
