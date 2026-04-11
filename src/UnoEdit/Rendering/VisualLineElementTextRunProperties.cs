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

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Uno-specific stub for text run properties associated with a visual line element.
	/// Replaces WPF's TextRunProperties / TextRunTypographyProperties with a minimal
	/// Brush-based API that is sufficient for syntax-coloring use cases.
	/// </summary>
	public class VisualLineElementTextRunProperties
	{
		/// <summary>Creates a new instance.</summary>
		public VisualLineElementTextRunProperties() { }

		/// <summary>Gets or sets the foreground brush.</summary>
		public Brush ForegroundBrush { get; set; }

		/// <summary>Gets or sets the background brush.</summary>
		public Brush BackgroundBrush { get; set; }

		/// <summary>Gets the typeface (stub).</summary>
		public object Typeface { get; private set; }

		/// <summary>Gets the font rendering em size.</summary>
		public double FontRenderingEmSize { get; private set; }

		/// <summary>Gets the font hinting em size.</summary>
		public double FontHintingEmSize { get; private set; }

		/// <summary>Gets the text decorations (stub).</summary>
		public object TextDecorations { get; private set; }

		/// <summary>Gets the text effects (stub).</summary>
		public object TextEffects { get; private set; }

		/// <summary>Gets the culture info (stub).</summary>
		public System.Globalization.CultureInfo CultureInfo { get; private set; }

		/// <summary>Gets the number substitution (stub).</summary>
		public object NumberSubstitution { get; private set; }

		/// <summary>Gets the typography properties (stub).</summary>
		public object TypographyProperties { get; private set; }

		/// <summary>Gets the baseline alignment (stub).</summary>
		public object BaselineAlignment { get; private set; }

		/// <summary>Sets the foreground brush.</summary>
		public void SetForegroundBrush(Brush brush) { ForegroundBrush = brush; }

		/// <summary>Sets the background brush.</summary>
		public void SetBackgroundBrush(Brush brush) { BackgroundBrush = brush; }

		/// <summary>Sets the typeface (stub).</summary>
		public void SetTypeface(object typeface) { Typeface = typeface; }

		/// <summary>Sets the font rendering em size.</summary>
		public void SetFontRenderingEmSize(double emSize) { FontRenderingEmSize = emSize; }

		/// <summary>Sets the font hinting em size.</summary>
		public void SetFontHintingEmSize(double emSize) { FontHintingEmSize = emSize; }

		/// <summary>Sets the text decorations (stub).</summary>
		public void SetTextDecorations(object textDecorations) { TextDecorations = textDecorations; }

		/// <summary>Sets the text effects (stub).</summary>
		public void SetTextEffects(object textEffects) { TextEffects = textEffects; }

		/// <summary>Sets the culture info.</summary>
		public void SetCultureInfo(System.Globalization.CultureInfo culture) { CultureInfo = culture; }

		/// <summary>Sets the number substitution (stub).</summary>
		public void SetNumberSubstitution(object numberSubstitution) { NumberSubstitution = numberSubstitution; }

		/// <summary>Sets the typography properties (stub).</summary>
		public void SetTypographyProperties(object typographyProperties) { TypographyProperties = typographyProperties; }

		/// <summary>Sets the baseline alignment (stub).</summary>
		public void SetBaselineAlignment(object baselineAlignment) { BaselineAlignment = baselineAlignment; }

		/// <summary>Creates a shallow clone of this instance.</summary>
		public VisualLineElementTextRunProperties Clone()
		{
			return new VisualLineElementTextRunProperties {
				ForegroundBrush = ForegroundBrush,
				BackgroundBrush = BackgroundBrush,
				FontRenderingEmSize = FontRenderingEmSize,
				FontHintingEmSize = FontHintingEmSize,
				CultureInfo = CultureInfo,
			};
		}
	}
}
