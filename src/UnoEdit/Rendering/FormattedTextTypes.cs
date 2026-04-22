// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Windows.Media.TextFormatting;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.Rendering
{

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
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
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
	public class FormattedTextRun : TextEmbeddedObject
	{
		/// <summary>Creates a FormattedTextRun.</summary>
		public FormattedTextRun(FormattedTextElement element, TextRunProperties properties)
		{
			Element = element;
			Properties = properties;
		}

		/// <summary>Gets the element that created this run.</summary>
		public FormattedTextElement Element { get; }

		/// <summary>Gets the break condition before.</summary>
		public override LineBreakCondition BreakBefore => Element.BreakBefore;

		/// <summary>Gets the break condition after.</summary>
		public override LineBreakCondition BreakAfter => Element.BreakAfter;

		/// <summary>Gets whether this has a fixed size.</summary>
		public override bool HasFixedSize => true;

		/// <summary>Gets the prepared text content used by this run.</summary>
		public override CharacterBufferReference CharacterBufferReference => CharacterBufferReference.Empty;

		/// <summary>Gets the length.</summary>
		public override int Length => Element.VisualLength;

		/// <summary>Gets the run properties.</summary>
		public override TextRunProperties Properties { get; }

		/// <summary>Formats the run into a lightweight descriptor consumable by the Uno renderer.</summary>
		public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
		{
			string text = Element.PreparedText?.Text ?? string.Empty;
			double width = Math.Max(1d, text.Length);
			return new TextEmbeddedObjectMetrics(width, 1d, 1d);
		}

		/// <summary>Computes the bounding box based on prepared text length.</summary>
		public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
		{
			string text = Element.PreparedText?.Text ?? string.Empty;
			double width = Math.Max(1d, text.Length);
			return new Rect(0, 0, width, 1d);
		}

		/// <summary>Draws the run into a recording drawing context.</summary>
		public override void Draw(System.Windows.Media.DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
		{
			drawingContext?.Record("formatted-text", new
			{
				Text = Element.PreparedText?.Text ?? string.Empty,
				Origin = origin,
				RightToLeft = rightToLeft,
				Sideways = sideways
			});
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
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new InlineObjectRun(VisualLength, context?.GlobalTextRunProperties ?? TextRunProperties, Element) {
				VisualLine = context?.VisualLine
			};
		}
	}

	/// <summary>
	/// TextRun for <see cref="InlineObjectElement"/> within UnoEdit's reduced formatting model.
	/// </summary>
	public class InlineObjectRun : TextEmbeddedObject
	{
		/// <summary>Creates a new InlineObjectRun.</summary>
		public InlineObjectRun(int length, TextRunProperties properties, UIElement element)
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
		public override LineBreakCondition BreakBefore => LineBreakCondition.BreakDesired;

		/// <summary>Gets the break condition after.</summary>
		public override LineBreakCondition BreakAfter => LineBreakCondition.BreakDesired;

		/// <summary>Gets whether this has a fixed size.</summary>
		public override bool HasFixedSize => true;

		/// <summary>Gets the inline element referenced by this run.</summary>
		public override CharacterBufferReference CharacterBufferReference => CharacterBufferReference.Empty;

		/// <summary>Gets the length.</summary>
		public override int Length { get; }

		/// <summary>Gets the run properties.</summary>
		public override TextRunProperties Properties { get; }

		/// <summary>Formats the run into a lightweight descriptor consumable by the Uno renderer.</summary>
		public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
		{
			double width = Math.Max(1d, Length);
			return new TextEmbeddedObjectMetrics(width, 1d, 1d);
		}

		/// <summary>Computes the bounding box for the inline object.</summary>
		public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => new Rect(0, 0, Math.Max(1d, Length), 1d);

		/// <summary>Draws the run into a recording drawing context.</summary>
		public override void Draw(System.Windows.Media.DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
		{
			drawingContext?.Record("inline-object", new
			{
				Element,
				Origin = origin,
				RightToLeft = rightToLeft,
				Sideways = sideways
			});
		}
	}
}
