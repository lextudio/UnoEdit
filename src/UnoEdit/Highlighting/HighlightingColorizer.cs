using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Simplified UnoEdit implementation of HighlightingColorizer.
	/// </summary>
	public class HighlightingColorizer : Rendering.DocumentColorizingTransformer
	{
		readonly IHighlightingDefinition? definition;
		IHighlighter? highlighter;
		bool isFixedHighlighter;

		/// <summary>Creates a colorizer using the specified highlighting definition.</summary>
		public HighlightingColorizer(IHighlightingDefinition definition)
		{
			this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
		}

		/// <summary>Creates a colorizer using a fixed highlighter.</summary>
		public HighlightingColorizer(IHighlighter highlighter)
		{
			this.highlighter = highlighter ?? throw new ArgumentNullException(nameof(highlighter));
			isFixedHighlighter = true;
		}

		/// <summary>Creates a colorizer for subclassing.</summary>
		protected HighlightingColorizer()
		{
		}

		protected override void OnAddToTextView(TextView textView)
		{
			base.OnAddToTextView(textView);
			textView.DocumentChanged += TextView_DocumentChanged;
			EnsureHighlighter(textView);
		}

		protected override void OnRemoveFromTextView(TextView textView)
		{
			ReleaseHighlighter(textView);
			textView.DocumentChanged -= TextView_DocumentChanged;
			base.OnRemoveFromTextView(textView);
		}

		void TextView_DocumentChanged(object? sender, EventArgs e)
		{
			if (sender is TextView textView)
			{
				ReleaseHighlighter(textView);
				EnsureHighlighter(textView);
			}
		}

		void EnsureHighlighter(TextView textView)
		{
			if (textView.Document == null)
				return;

			if (!isFixedHighlighter)
				highlighter = CreateHighlighter(textView, textView.Document);

			if (highlighter != null && textView.Services.GetService(typeof(IHighlighter)) == null)
				textView.Services.AddService(typeof(IHighlighter), highlighter);
		}

		void ReleaseHighlighter(TextView textView)
		{
			if (textView.Services.GetService(typeof(IHighlighter)) == highlighter)
				textView.Services.RemoveService(typeof(IHighlighter));

			if (!isFixedHighlighter)
			{
				highlighter?.Dispose();
				highlighter = null;
			}
		}

		/// <summary>Creates the IHighlighter instance for the specified text document.</summary>
		protected virtual IHighlighter CreateHighlighter(TextView textView, TextDocument document)
		{
			if (definition == null)
				throw new NotSupportedException("No highlighting definition is available.");
			return new DocumentHighlighter(document, definition);
		}

		/// <inheritdoc/>
		protected override void ColorizeLine(DocumentLine line)
		{
			if (highlighter == null)
				return;

			HighlightedLine highlightedLine = highlighter.HighlightLine(line.LineNumber);
			foreach (HighlightedSection section in highlightedLine.Sections)
			{
				if (IsEmptyColor(section.Color))
					continue;

				ChangeLinePart(section.Offset, section.Offset + section.Length, element => ApplyColorToElement(element, section.Color, CurrentContext));
			}
		}

		internal static bool IsEmptyColor(HighlightingColor? color)
		{
			if (color == null)
				return true;
			return color.Background == null && color.Foreground == null
				&& color.FontStyle == null && color.FontWeight == null
				&& color.Underline == null && color.Strikethrough == null
				&& color.FontFamily == null && color.FontSize == null;
		}

		internal static void ApplyColorToElement(VisualLineElement element, HighlightingColor color, ITextRunConstructionContext? context)
		{
			if (color.Foreground != null && context != null)
			{
				var brush = color.Foreground.GetBrush(context);
				if (brush != null)
					element.TextRunProperties.SetForegroundBrush(brush);
			}
			if (color.Background != null && context != null)
			{
				var brush = color.Background.GetBrush(context);
				if (brush != null)
					element.BackgroundBrush = brush;
			}
			if (color.FontFamily != null || color.FontStyle != null || color.FontWeight != null)
			{
				var current = element.TextRunProperties.Typeface ?? new System.Windows.Media.Typeface("Default");
				element.TextRunProperties.SetTypeface(new System.Windows.Media.Typeface(
					color.FontFamily ?? current.FontFamily,
					color.FontStyle ?? current.Style,
					color.FontWeight ?? current.Weight,
					current.Stretch));
			}
			if (color.Underline == true)
				element.TextRunProperties.SetTextDecorations(System.Windows.Media.TextDecorations.Underline);
			if (color.Strikethrough == true)
				element.TextRunProperties.SetTextDecorations(System.Windows.Media.TextDecorations.Strikethrough);
			if (color.FontSize.HasValue)
				element.TextRunProperties.SetFontRenderingEmSize(color.FontSize.Value);
		}
	}
}
