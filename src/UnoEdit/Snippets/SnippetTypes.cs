// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using System.Reflection;
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
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!string.IsNullOrEmpty(Text))
				context.InsertText(Text);
		}

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>Marks a caret position.</summary>
	[Serializable]
	public class SnippetCaretElement : SnippetElement
	{
		private readonly bool setCaretOnlyIfTextIsSelected;

		/// <summary>Creates a new SnippetCaretElement.</summary>
		public SnippetCaretElement() : this(false) { }

		/// <summary>Creates a new SnippetCaretElement with the specified option.</summary>
		public SnippetCaretElement(bool setCaretOnlyIfTextIsSelected)
		{
			this.setCaretOnlyIfTextIsSelected = setCaretOnlyIfTextIsSelected;
		}

		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			if (setCaretOnlyIfTextIsSelected && string.IsNullOrEmpty(context.SelectedText))
				return;

			var caretOffsetProperty = context.TextArea?.GetType().GetProperty("CurrentOffset", BindingFlags.Public | BindingFlags.Instance);
			if (caretOffsetProperty != null && caretOffsetProperty.CanWrite)
				caretOffsetProperty.SetValue(context.TextArea, context.InsertionPosition);
		}
	}

	/// <summary>Container element that groups child snippet elements.</summary>
	[Serializable]
	public class SnippetContainerElement : SnippetElement
	{
		/// <summary>Gets the child elements.</summary>
		public IList<SnippetElement> Elements { get; } = new List<SnippetElement>();

		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			foreach (var element in Elements)
				element?.Insert(context);
		}

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
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			var offset = context.InsertionPosition;
			var anchor = context.Document.CreateAnchor(offset);
			anchor.MovementType = AnchorMovementType.BeforeInsertion;
			anchor.SurviveDeletion = true;
			context.RegisterActiveElement(this, new AnchorElement(new AnchorSegment(anchor, anchor), Name, context));
		}
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
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			var active = TargetElement != null ? context.GetActiveElement(TargetElement) as IReplaceableActiveElement : null;
			var text = active != null ? ConvertText(active.Text) : string.Empty;
			context.InsertText(text ?? string.Empty);
		}

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
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			var text = context.SelectedText ?? string.Empty;
			if (text.Length == 0)
				return;

			if (Indentation > 0)
				text = text.Replace(context.LineTerminator, context.LineTerminator + new string(' ', Indentation));

			context.InsertText(text);
		}
	}

	/// <summary>A replaceable text element in a snippet.</summary>
	[Serializable]
	public class SnippetReplaceableTextElement : SnippetTextElement
	{
		/// <inheritdoc/>
		public override void Insert(InsertionContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			var start = context.InsertionPosition;
			base.Insert(context);
			var end = context.InsertionPosition;

			var startAnchor = context.Document.CreateAnchor(start);
			startAnchor.MovementType = AnchorMovementType.BeforeInsertion;
			startAnchor.SurviveDeletion = true;

			var endAnchor = context.Document.CreateAnchor(end);
			endAnchor.MovementType = AnchorMovementType.BeforeInsertion;
			endAnchor.SurviveDeletion = true;

			context.RegisterActiveElement(this, new ReplaceableActiveElement(new AnchorSegment(startAnchor, endAnchor), Text ?? string.Empty));
		}

		/// <inheritdoc/>
		public override object ToTextRun() => null;
	}

	/// <summary>A code snippet that can be inserted into the text editor.</summary>
	[Serializable]
	public class Snippet : SnippetContainerElement
	{
		/// <summary>Inserts the snippet into the text area.</summary>
		public void Insert(object textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException(nameof(textArea));

			var currentOffsetProperty = textArea.GetType().GetProperty("CurrentOffset", BindingFlags.Public | BindingFlags.Instance);
			var insertionOffset = currentOffsetProperty?.GetValue(textArea) is int offset ? offset : 0;
			var context = new InsertionContext(textArea, insertionOffset);
			Insert(context);
			context.RaiseInsertionCompleted(EventArgs.Empty);
		}
	}

	/// <summary>Active element registered by <see cref="SnippetAnchorElement"/>.</summary>
	public sealed class AnchorElement : IActiveElement
	{
		private readonly InsertionContext context;
		private readonly ISegment segment;

		/// <summary>Creates a new AnchorElement.</summary>
		public AnchorElement(object segment, string name, InsertionContext context)
		{
			this.segment = segment as ISegment;
			this.context = context;
			Name = name;
		}

		/// <inheritdoc/>
		public bool IsEditable => false;

		/// <inheritdoc/>
				public ISegment Segment => segment;

		/// <summary>Gets the name of this anchor.</summary>
		public string Name { get; private set; }

		/// <summary>Gets the text at this anchor.</summary>
				public string Text => Segment != null ? context.Document.GetText(Segment.Offset, Segment.Length) : string.Empty;

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

	sealed class ReplaceableActiveElement : IReplaceableActiveElement
	{
		readonly ISegment segment;
		readonly string text;

		public ReplaceableActiveElement(ISegment segment, string text)
		{
			this.segment = segment;
			this.text = text ?? string.Empty;
		}

		public string Text => text;
		public bool IsEditable => true;
		public ISegment Segment => segment;
		public event EventHandler TextChanged;
		public void OnInsertionCompleted() { }
		public void Deactivate(SnippetEventArgs e) { }
	}
}
