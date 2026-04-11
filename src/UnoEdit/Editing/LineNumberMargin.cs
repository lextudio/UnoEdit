// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — stub for API parity.

using System;
using System.ComponentModel;
using System.Globalization;
using Windows.Foundation;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Margin that displays line numbers.
	/// </summary>
	public class LineNumberMargin : AbstractMargin
	{
		int maxLineNumberLength = 2;

		/// <summary>Creates a new LineNumberMargin.</summary>
		public LineNumberMargin() { }

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			// Keep the same behavior as AvalonEdit: reserve enough width for the max line number.
			// We estimate character width because the Uno port does not expose WPF FormattedText.
			var charWidth = 8.0;
			return new Size(charWidth * maxLineNumberLength + 8.0, 0);
		}

		/// <inheritdoc/>
		protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
		{
			if (oldTextView != null)
				oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;

			base.OnTextViewChanged(oldTextView, newTextView);

			if (newTextView != null)
				newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
		}

		/// <inheritdoc/>
		protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
		{
			if (oldDocument is INotifyPropertyChanged oldNotify)
				oldNotify.PropertyChanged -= DocumentPropertyChanged;

			base.OnDocumentChanged(oldDocument, newDocument);

			if (newDocument is INotifyPropertyChanged newNotify)
				newNotify.PropertyChanged += DocumentPropertyChanged;

			OnDocumentLineCountChanged();
		}

		void DocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (string.Equals(e.PropertyName, nameof(TextDocument.LineCount), StringComparison.Ordinal))
				OnDocumentLineCountChanged();
		}

		void OnDocumentLineCountChanged()
		{
			var count = Document != null ? Document.LineCount : 1;
			var newLength = Math.Max(2, count.ToString(CultureInfo.CurrentCulture).Length);
			if (newLength != maxLineNumberLength) {
				maxLineNumberLength = newLength;
				InvalidateMeasure();
			}
			InvalidateArrange();
		}

		void TextViewVisualLinesChanged(object sender, EventArgs e)
		{
			InvalidateArrange();
		}
	}
}
