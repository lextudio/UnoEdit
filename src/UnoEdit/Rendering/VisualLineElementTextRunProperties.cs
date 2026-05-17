// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — extends the LeXtudio.Windows TextRunProperties shim.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// <see cref="TextRunProperties"/> implementation that allows changing individual properties.
	/// A <see cref="VisualLineElementTextRunProperties"/> instance is usually assigned to a single
	/// <see cref="VisualLineElement"/>.
	/// </summary>
	public class VisualLineElementTextRunProperties : TextRunProperties, ICloneable
	{
		Brush backgroundBrush;
		BaselineAlignment baselineAlignment = BaselineAlignment.Baseline;
		CultureInfo cultureInfo = CultureInfo.CurrentCulture;
		double fontHintingEmSize = 12d;
		double fontRenderingEmSize = 12d;
		Brush foregroundBrush;
		Typeface typeface = new Typeface("Segoe UI");
		TextDecorationCollection textDecorations;
		TextEffectCollection textEffects;
		TextRunTypographyProperties typographyProperties;
		NumberSubstitution numberSubstitution;

		/// <summary>Creates a new instance with default values.</summary>
		public VisualLineElementTextRunProperties() { }

		/// <summary>
		/// Creates a new instance that copies its values from <paramref name="textRunProperties"/>.
		/// </summary>
		public VisualLineElementTextRunProperties(TextRunProperties textRunProperties)
		{
			if (textRunProperties == null)
				throw new ArgumentNullException("textRunProperties");
			backgroundBrush = textRunProperties.BackgroundBrush;
			baselineAlignment = textRunProperties.BaselineAlignment;
			cultureInfo = textRunProperties.CultureInfo ?? CultureInfo.CurrentCulture;
			fontHintingEmSize = textRunProperties.FontHintingEmSize;
			fontRenderingEmSize = textRunProperties.FontRenderingEmSize;
			foregroundBrush = textRunProperties.ForegroundBrush;
			typeface = textRunProperties.Typeface ?? new Typeface("Segoe UI");
			textDecorations = textRunProperties.TextDecorations;
			if (textDecorations != null && !textDecorations.IsFrozen)
				textDecorations = textDecorations.Clone();
			textEffects = textRunProperties.TextEffects;
			if (textEffects != null && !textEffects.IsFrozen)
				textEffects = textEffects.Clone();
			typographyProperties = textRunProperties.TypographyProperties;
			numberSubstitution = textRunProperties.NumberSubstitution;
		}

		/// <summary>Creates a copy of this instance.</summary>
		public virtual VisualLineElementTextRunProperties Clone()
		{
			return new VisualLineElementTextRunProperties(this);
		}

		object ICloneable.Clone() => Clone();

		/// <inheritdoc/>
		public override Brush BackgroundBrush => backgroundBrush;

		/// <inheritdoc/>
		public override BaselineAlignment BaselineAlignment => baselineAlignment;

		/// <inheritdoc/>
		public override CultureInfo CultureInfo => cultureInfo;

		/// <inheritdoc/>
		public override double FontHintingEmSize => fontHintingEmSize;

		/// <inheritdoc/>
		public override double FontRenderingEmSize => fontRenderingEmSize;

		/// <inheritdoc/>
		public override Brush ForegroundBrush => foregroundBrush;

		/// <inheritdoc/>
		public override Typeface Typeface => typeface;

		/// <inheritdoc/>
		public override TextDecorationCollection TextDecorations => textDecorations;

		/// <inheritdoc/>
		public override TextEffectCollection TextEffects => textEffects;

		/// <inheritdoc/>
		public override NumberSubstitution NumberSubstitution => numberSubstitution;

		/// <inheritdoc/>
		public override TextRunTypographyProperties TypographyProperties => typographyProperties;

		public void SetForegroundBrush(Brush value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			foregroundBrush = value;
		}

		public void SetBackgroundBrush(Brush value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			backgroundBrush = value;
		}

		public void SetBaselineAlignment(BaselineAlignment value) { baselineAlignment = value; }

		public void SetCultureInfo(CultureInfo value)
		{
			if (value == null) throw new ArgumentNullException("value");
			cultureInfo = value;
		}

		public void SetFontHintingEmSize(double value) { fontHintingEmSize = value; }

		public void SetFontRenderingEmSize(double value) { fontRenderingEmSize = value; }

		public void SetTypeface(Typeface value)
		{
			if (value == null) throw new ArgumentNullException("value");
			typeface = value;
		}

		public void SetTextDecorations(TextDecorationCollection value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			if (textDecorations == null)
				textDecorations = value;
			else
				textDecorations = new TextDecorationCollection(System.Linq.Enumerable.Union(textDecorations, value));
		}

		public void SetTextEffects(TextEffectCollection value)
		{
			ExtensionMethods.CheckIsFrozen(value);
			textEffects = value;
		}

		public void SetTypographyProperties(TextRunTypographyProperties value) { typographyProperties = value; }

		public void SetNumberSubstitution(NumberSubstitution value) { numberSubstitution = value; }
	}
}
