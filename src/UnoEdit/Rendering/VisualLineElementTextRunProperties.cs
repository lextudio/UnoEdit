// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Shared text-run property bag for UnoEdit rendering.
	/// This keeps AvalonEdit's mutable property model while using Uno-friendly descriptors
	/// instead of WPF text-formatting types.
	/// </summary>
	public class VisualLineElementTextRunProperties
	{
		public sealed class TypefaceDescriptor : IEquatable<TypefaceDescriptor>
		{
			public static readonly TypefaceDescriptor Default = new TypefaceDescriptor("Default", "Normal", "Normal", "Normal");

			public TypefaceDescriptor(string fontFamily, string style, string weight, string stretch)
			{
				FontFamily = fontFamily ?? "Default";
				Style = style ?? "Normal";
				Weight = weight ?? "Normal";
				Stretch = stretch ?? "Normal";
			}

			public string FontFamily { get; }
			public string Style { get; }
			public string Weight { get; }
			public string Stretch { get; }

			public bool Equals(TypefaceDescriptor other)
				=> other != null
					&& FontFamily == other.FontFamily
					&& Style == other.Style
					&& Weight == other.Weight
					&& Stretch == other.Stretch;

			public override bool Equals(object obj) => Equals(obj as TypefaceDescriptor);

			public override int GetHashCode() => HashCode.Combine(FontFamily, Style, Weight, Stretch);
		}

		public sealed class NumberSubstitutionDescriptor : IEquatable<NumberSubstitutionDescriptor>
		{
			public static readonly NumberSubstitutionDescriptor Default = new NumberSubstitutionDescriptor("Culture", "Traditional");

			public NumberSubstitutionDescriptor(string cultureSource, string substitutionMethod)
			{
				CultureSource = cultureSource ?? "Culture";
				SubstitutionMethod = substitutionMethod ?? "Traditional";
			}

			public string CultureSource { get; }
			public string SubstitutionMethod { get; }

			public bool Equals(NumberSubstitutionDescriptor other)
				=> other != null
					&& CultureSource == other.CultureSource
					&& SubstitutionMethod == other.SubstitutionMethod;

			public override bool Equals(object obj) => Equals(obj as NumberSubstitutionDescriptor);

			public override int GetHashCode() => HashCode.Combine(CultureSource, SubstitutionMethod);
		}

		/// <summary>Creates a new instance with concrete defaults.</summary>
		public VisualLineElementTextRunProperties()
		{
			Typeface = TypefaceDescriptor.Default;
			FontRenderingEmSize = 12d;
			FontHintingEmSize = 12d;
			TextDecorations = Array.Empty<string>();
			TextEffects = Array.Empty<object>();
			CultureInfo = CultureInfo.CurrentCulture;
			NumberSubstitution = NumberSubstitutionDescriptor.Default;
			TypographyProperties = DefaultTextRunTypographyProperties.Default;
			BaselineAlignment = "Baseline";
		}

		/// <summary>Gets or sets the foreground brush.</summary>
		public Brush ForegroundBrush { get; set; }

		/// <summary>Gets or sets the background brush.</summary>
		public Brush BackgroundBrush { get; set; }

		/// <summary>Gets the typeface descriptor.</summary>
		public object Typeface { get; private set; }

		/// <summary>Gets the font rendering em size.</summary>
		public double FontRenderingEmSize { get; private set; }

		/// <summary>Gets the font hinting em size.</summary>
		public double FontHintingEmSize { get; private set; }

		/// <summary>Gets the text decorations collection.</summary>
		public object TextDecorations { get; private set; }

		/// <summary>Gets the text effects collection.</summary>
		public object TextEffects { get; private set; }

		/// <summary>Gets the culture info.</summary>
		public CultureInfo CultureInfo { get; private set; }

		/// <summary>Gets the number substitution descriptor.</summary>
		public object NumberSubstitution { get; private set; }

		/// <summary>Gets the typography properties descriptor.</summary>
		public object TypographyProperties { get; private set; }

		/// <summary>Gets the baseline alignment descriptor.</summary>
		public object BaselineAlignment { get; private set; }

		/// <summary>Sets the foreground brush.</summary>
		public void SetForegroundBrush(Brush brush) { ForegroundBrush = brush; }

		/// <summary>Sets the background brush.</summary>
		public void SetBackgroundBrush(Brush brush) { BackgroundBrush = brush; }

		/// <summary>Sets the typeface descriptor.</summary>
		public void SetTypeface(object typeface) { Typeface = typeface ?? TypefaceDescriptor.Default; }

		/// <summary>Sets the font rendering em size.</summary>
		public void SetFontRenderingEmSize(double emSize) { FontRenderingEmSize = emSize; }

		/// <summary>Sets the font hinting em size.</summary>
		public void SetFontHintingEmSize(double emSize) { FontHintingEmSize = emSize; }

		/// <summary>Sets the text decorations collection.</summary>
		public void SetTextDecorations(object textDecorations)
		{
			if (textDecorations is null)
				TextDecorations = Array.Empty<string>();
			else if (textDecorations is string singleDecoration)
				TextDecorations = new[] { singleDecoration };
			else
				TextDecorations = textDecorations;
		}

		/// <summary>Sets the text effects collection.</summary>
		public void SetTextEffects(object textEffects) { TextEffects = textEffects ?? Array.Empty<object>(); }

		/// <summary>Sets the culture info.</summary>
		public void SetCultureInfo(CultureInfo culture) { CultureInfo = culture ?? CultureInfo.CurrentCulture; }

		/// <summary>Sets the number substitution descriptor.</summary>
		public void SetNumberSubstitution(object numberSubstitution) { NumberSubstitution = numberSubstitution ?? NumberSubstitutionDescriptor.Default; }

		/// <summary>Sets the typography properties descriptor.</summary>
		public void SetTypographyProperties(object typographyProperties) { TypographyProperties = typographyProperties ?? DefaultTextRunTypographyProperties.Default; }

		/// <summary>Sets the baseline alignment descriptor.</summary>
		public void SetBaselineAlignment(object baselineAlignment) { BaselineAlignment = baselineAlignment ?? "Baseline"; }

		/// <summary>Creates a clone of this instance.</summary>
		public VisualLineElementTextRunProperties Clone()
		{
			return new VisualLineElementTextRunProperties {
				ForegroundBrush = ForegroundBrush,
				BackgroundBrush = BackgroundBrush,
				Typeface = Typeface,
				FontRenderingEmSize = FontRenderingEmSize,
				FontHintingEmSize = FontHintingEmSize,
				TextDecorations = CloneCollection(TextDecorations),
				TextEffects = CloneCollection(TextEffects),
				CultureInfo = CultureInfo,
				NumberSubstitution = NumberSubstitution,
				TypographyProperties = TypographyProperties,
				BaselineAlignment = BaselineAlignment,
			};
		}

		static object CloneCollection(object value)
		{
			if (value is null)
				return Array.Empty<object>();
			if (value is string)
				return value;
			if (value is IEnumerable<string> strings)
				return new List<string>(strings);
			if (value is IEnumerable<object> objects)
				return new List<object>(objects);
			return value;
		}
	}
}
