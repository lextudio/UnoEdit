// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>Compatibility enum for WPF's LineBreakCondition.</summary>
	public enum LineBreakCondition { BreakDesired, BreakPossible, BreakRestrained, BreakAlways }

	/// <summary>
	/// VisualLineElement that displays prepared formatted text within UnoEdit's reduced text-formatting model.
	/// </summary>
	public class FormattedTextElement : VisualLineElement
	{
		public sealed class PreparedTextDescriptor
		{
			public PreparedTextDescriptor(object formatter, string text, object properties)
			{
				Formatter = formatter;
				Text = text ?? string.Empty;
				Properties = properties;
			}

			public object Formatter { get; }
			public string Text { get; }
			public object Properties { get; }
		}

		public sealed class FormattedRunMetadata
		{
			public FormattedRunMetadata(double remainingParagraphWidth, PreparedTextDescriptor? preparedText)
			{
				RemainingParagraphWidth = remainingParagraphWidth;
				PreparedText = preparedText;
			}

			public double RemainingParagraphWidth { get; }
			public PreparedTextDescriptor? PreparedText { get; }
		}

		/// <summary>Creates a FormattedTextElement from text/document content.</summary>
		public FormattedTextElement(int documentLength) : base(1, documentLength) { }

		/// <summary>Gets or sets prepared text associated with this element.</summary>
		public PreparedTextDescriptor? PreparedText { get; set; }

		/// <summary>Gets/sets the line break condition before this element.</summary>
		public LineBreakCondition BreakBefore { get; set; }

		/// <summary>Gets/sets the line break condition after this element.</summary>
		public LineBreakCondition BreakAfter { get; set; }

		/// <inheritdoc/>
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new FormattedTextRun(this, context?.GlobalTextRunProperties ?? TextRunProperties);
		}

		/// <summary>Prepares text for later formatting and drawing in the Uno compatibility pipeline.</summary>
		public static object PrepareText(object formatter, string text, object properties)
		{
			return new PreparedTextDescriptor(formatter, text, properties);
		}
	}

	/// <summary>
	/// TextRun for <see cref="FormattedTextElement"/> within UnoEdit's reduced formatting model.
	/// </summary>
	public class FormattedTextRun
	{
		/// <summary>Creates a FormattedTextRun.</summary>
		public FormattedTextRun(FormattedTextElement element, object properties)
		{
			Element = element;
			Properties = properties;
		}

		/// <summary>Gets the element that created this run.</summary>
		public FormattedTextElement Element { get; }

		/// <summary>Gets the break condition before.</summary>
		public LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;

		/// <summary>Gets the break condition after.</summary>
		public LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;

		/// <summary>Gets whether this has a fixed size.</summary>
		public bool HasFixedSize => true;

		/// <summary>Gets the prepared text content used by this run.</summary>
		public object CharacterBufferReference => Element.PreparedText?.Text ?? string.Empty;

		/// <summary>Gets the length.</summary>
		public int Length => 1;

		/// <summary>Gets the run properties.</summary>
		public object Properties { get; }

		/// <summary>Formats the run into a lightweight descriptor consumable by the Uno renderer.</summary>
		public object Format(double remainingParagraphWidth)
		{
			return new VisualLineElement.TextRunDescriptor(
				"formatted",
				Element.PreparedText?.Text ?? string.Empty,
				0,
				Length,
				Element.TextRunProperties,
				new FormattedTextElement.FormattedRunMetadata(remainingParagraphWidth, Element.PreparedText));
		}

		/// <summary>Computes the bounding box based on prepared text length.</summary>
		public Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
		{
			string text = Element.PreparedText?.Text ?? string.Empty;
			double width = Math.Max(1d, text.Length);
			return new Rect(0, 0, width, 1d);
		}

		/// <summary>Draws the run into a recording drawing context.</summary>
		public void Draw(object drawingContext, object origin, bool rightToLeft, bool sideways)
		{
			if (drawingContext is System.Windows.Media.DrawingContext dc)
			{
				dc.Record("formatted-text", new
				{
					Text = Element.PreparedText?.Text ?? string.Empty,
					Origin = origin,
					RightToLeft = rightToLeft,
					Sideways = sideways
				});
			}
		}
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
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new InlineObjectRun(VisualLength, context?.GlobalTextRunProperties ?? TextRunProperties, Element) {
				VisualLine = context?.VisualLine
			};
		}
	}

	/// <summary>
	/// TextRun for <see cref="InlineObjectElement"/> within UnoEdit's reduced formatting model.
	/// </summary>
	public class InlineObjectRun
	{
		/// <summary>Creates a new InlineObjectRun.</summary>
		public InlineObjectRun(int length, object properties, UIElement element)
		{
			Length = length;
			Element = element;
			Properties = properties;
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

		/// <summary>Gets the inline element referenced by this run.</summary>
		public object CharacterBufferReference => Element;

		/// <summary>Gets the length.</summary>
		public int Length { get; }

		/// <summary>Gets the run properties.</summary>
		public object Properties { get; }

		/// <summary>Formats the run into a lightweight descriptor consumable by the Uno renderer.</summary>
		public object Format(double remainingParagraphWidth)
		{
			return new VisualLineElement.TextRunDescriptor(
				"inline-object",
				string.Empty,
				0,
				Length,
				Properties as VisualLineElementTextRunProperties,
				new { RemainingParagraphWidth = remainingParagraphWidth, Element, VisualLine });
		}

		/// <summary>Computes the bounding box for the inline object.</summary>
		public Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => new Rect(0, 0, Math.Max(1d, Length), 1d);

		/// <summary>Draws the run into a recording drawing context.</summary>
		public void Draw(object drawingContext, object origin, bool rightToLeft, bool sideways)
		{
			if (drawingContext is System.Windows.Media.DrawingContext dc)
			{
				dc.Record("inline-object", new
				{
					Element,
					Origin = origin,
					RightToLeft = rightToLeft,
					Sideways = sideways
				});
			}
		}
	}
}
