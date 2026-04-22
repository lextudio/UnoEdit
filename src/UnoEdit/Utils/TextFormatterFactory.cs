using System;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Utils
{
	static class TextFormatterFactory
	{
		public static TextFormatter Create(object owner)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			return TextFormatter.Create();
		}

		public static bool PropertyChangeAffectsTextFormatter(DependencyProperty dp)
		{
			return false;
		}

		public static FormattedText CreateFormattedText(FrameworkElement element, string text, Typeface typeface, double? emSize, Brush foreground)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			typeface ??= new Typeface("Consolas");
			emSize ??= 12.0;
			foreground ??= Brushes.Black;

			return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, emSize.Value, foreground);
		}
	}
}
