// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>Stub for WPF's LineBreakCondition.</summary>
	public enum LineBreakCondition { BreakDesired, BreakPossible, BreakRestrained, BreakAlways }

	/// <summary>
	/// VisualLineElement that displays formatted text using the WPF text formatting pipeline.
	/// Stub implementation — WPF TextFormatter dependencies not available in Uno.
	/// </summary>
	public class FormattedTextElement : VisualLineElement
	{
		/// <summary>Creates a FormattedTextElement from text/document content.</summary>
		public FormattedTextElement(int documentLength) : base(1, documentLength) { }

		/// <summary>Gets/sets the line break condition before this element.</summary>
		public LineBreakCondition BreakBefore { get; set; }

		/// <summary>Gets/sets the line break condition after this element.</summary>
		public LineBreakCondition BreakAfter { get; set; }

		/// <inheritdoc/>
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context) => null;

		/// <summary>Prepares a text line (stub).</summary>
		public static object PrepareText(object formatter, string text, object properties) => null;
	}

	/// <summary>
	/// TextRun for FormattedTextElement. Stub — WPF TextEmbeddedObject not available in Uno.
	/// </summary>
	public class FormattedTextRun
	{
		/// <summary>Creates a FormattedTextRun.</summary>
		public FormattedTextRun(FormattedTextElement element, object properties)
		{
			Element = element;
		}

		/// <summary>Gets the element that created this run.</summary>
		public FormattedTextElement Element { get; }

		/// <summary>Gets the break condition before.</summary>
		public LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;

		/// <summary>Gets the break condition after.</summary>
		public LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;

		/// <summary>Gets whether this has a fixed size.</summary>
		public bool HasFixedSize => true;

		/// <summary>Gets the character buffer reference (stub).</summary>
		public object CharacterBufferReference => null;

		/// <summary>Gets the length.</summary>
		public int Length => 1;

		/// <summary>Gets the run properties.</summary>
		public object Properties { get; }

		/// <summary>Formats the run (stub).</summary>
		public object Format(double remainingParagraphWidth) => null;

		/// <summary>Computes the bounding box (stub).</summary>
		public Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => Rect.Empty;

		/// <summary>Draws the run (stub).</summary>
		public void Draw(object drawingContext, object origin, bool rightToLeft, bool sideways) { }
	}

	/// <summary>
	/// VisualLineElement for inline UIElements within the text.
	/// </summary>
	public class InlineObjectElement : VisualLineElement
	{
		/// <summary>Gets the inline element.</summary>
		public UIElement Element { get; private set; }

		/// <summary>Creates a new InlineObjectElement.</summary>
		public InlineObjectElement(int documentLength, UIElement element) : base(1, documentLength)
		{
			Element = element;
		}

		/// <inheritdoc/>
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context) => null;
	}

	/// <summary>
	/// TextRun for InlineObjectElement. Stub — WPF TextEmbeddedObject not available in Uno.
	/// </summary>
	public class InlineObjectRun
	{
		/// <summary>Creates a new InlineObjectRun.</summary>
		public InlineObjectRun(int length, object properties, UIElement element)
		{
			Length = length;
			Element = element;
		}

		/// <summary>Gets the inline element.</summary>
		public UIElement Element { get; }

		/// <summary>Gets the visual line that owns this run.</summary>
		public VisualLine VisualLine { get; internal set; }

		/// <summary>Gets the break condition before.</summary>
		public LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;

		/// <summary>Gets the break condition after.</summary>
		public LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;

		/// <summary>Gets whether this has a fixed size.</summary>
		public bool HasFixedSize => true;

		/// <summary>Gets the character buffer reference (stub).</summary>
		public object CharacterBufferReference => null;

		/// <summary>Gets the length.</summary>
		public int Length { get; }

		/// <summary>Gets the run properties.</summary>
		public object Properties { get; }

		/// <summary>Formats the run (stub).</summary>
		public object Format(double remainingParagraphWidth) => null;

		/// <summary>Computes the bounding box (stub).</summary>
		public Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => Rect.Empty;

		/// <summary>Draws the run (stub).</summary>
		public void Draw(object drawingContext, object origin, bool rightToLeft, bool sideways) { }
	}
}
