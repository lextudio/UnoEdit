using System;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Default typography properties for UnoEdit text runs.
	/// </summary>
	public class DefaultTextRunTypographyProperties
	{
		public static readonly DefaultTextRunTypographyProperties Default = new DefaultTextRunTypographyProperties();

		public enum TypographyKind
		{
			Default,
			Normal,
			Proportional
		}

		readonly int annotationAlternates;
		readonly object capitals;
		readonly bool capitalSpacing;
		readonly bool caseSensitiveForms;
		readonly bool contextualAlternates;
		readonly bool contextualLigatures;
		readonly int contextualSwashes;
		readonly bool discretionaryLigatures;
		readonly bool eastAsianExpertForms;
		readonly object eastAsianLanguage;
		readonly object eastAsianWidths;
		readonly object fraction;
		readonly bool historicalForms;
		readonly bool historicalLigatures;
		readonly bool kerning;
		readonly bool mathematicalGreek;
		readonly object numeralAlignment;
		readonly object numeralStyle;
		readonly bool slashedZero;
		readonly bool standardLigatures;
		readonly int standardSwashes;
		readonly int stylisticAlternates;
		readonly object variants;
		readonly bool[] stylisticSets;

		public DefaultTextRunTypographyProperties()
			: this(
				annotationAlternates: 0,
				capitals: TypographyKind.Default,
				capitalSpacing: false,
				caseSensitiveForms: false,
				contextualAlternates: true,
				contextualLigatures: true,
				contextualSwashes: 0,
				discretionaryLigatures: false,
				eastAsianExpertForms: false,
				eastAsianLanguage: TypographyKind.Default,
				eastAsianWidths: TypographyKind.Normal,
				fraction: TypographyKind.Default,
				historicalForms: false,
				historicalLigatures: false,
				kerning: true,
				mathematicalGreek: false,
				numeralAlignment: TypographyKind.Proportional,
				numeralStyle: TypographyKind.Normal,
				slashedZero: false,
				standardLigatures: true,
				standardSwashes: 0,
				stylisticAlternates: 0,
				variants: TypographyKind.Normal,
				stylisticSets: null)
		{
		}

		internal DefaultTextRunTypographyProperties(
			int annotationAlternates,
			object capitals,
			bool capitalSpacing,
			bool caseSensitiveForms,
			bool contextualAlternates,
			bool contextualLigatures,
			int contextualSwashes,
			bool discretionaryLigatures,
			bool eastAsianExpertForms,
			object eastAsianLanguage,
			object eastAsianWidths,
			object fraction,
			bool historicalForms,
			bool historicalLigatures,
			bool kerning,
			bool mathematicalGreek,
			object numeralAlignment,
			object numeralStyle,
			bool slashedZero,
			bool standardLigatures,
			int standardSwashes,
			int stylisticAlternates,
			object variants,
			bool[]? stylisticSets)
		{
			this.annotationAlternates = Math.Max(0, annotationAlternates);
			this.capitals = capitals ?? TypographyKind.Default;
			this.capitalSpacing = capitalSpacing;
			this.caseSensitiveForms = caseSensitiveForms;
			this.contextualAlternates = contextualAlternates;
			this.contextualLigatures = contextualLigatures;
			this.contextualSwashes = Math.Max(0, contextualSwashes);
			this.discretionaryLigatures = discretionaryLigatures;
			this.eastAsianExpertForms = eastAsianExpertForms;
			this.eastAsianLanguage = eastAsianLanguage ?? TypographyKind.Default;
			this.eastAsianWidths = eastAsianWidths ?? TypographyKind.Normal;
			this.fraction = fraction ?? TypographyKind.Default;
			this.historicalForms = historicalForms;
			this.historicalLigatures = historicalLigatures;
			this.kerning = kerning;
			this.mathematicalGreek = mathematicalGreek;
			this.numeralAlignment = numeralAlignment ?? TypographyKind.Proportional;
			this.numeralStyle = numeralStyle ?? TypographyKind.Normal;
			this.slashedZero = slashedZero;
			this.standardLigatures = standardLigatures;
			this.standardSwashes = Math.Max(0, standardSwashes);
			this.stylisticAlternates = Math.Max(0, stylisticAlternates);
			this.variants = variants ?? TypographyKind.Normal;
			this.stylisticSets = new bool[20];
			if (stylisticSets != null)
			{
				Array.Copy(stylisticSets, this.stylisticSets, Math.Min(stylisticSets.Length, this.stylisticSets.Length));
			}
		}

		/// <summary>Gets annotation alternates count.</summary>
		public int AnnotationAlternates => annotationAlternates;
		/// <summary>Gets font capitals setting.</summary>
		public object Capitals => capitals;
		/// <summary>Gets whether capital spacing is enabled.</summary>
		public bool CapitalSpacing => capitalSpacing;
		/// <summary>Gets whether case-sensitive forms are enabled.</summary>
		public bool CaseSensitiveForms => caseSensitiveForms;
		/// <summary>Gets whether contextual alternates are enabled.</summary>
		public bool ContextualAlternates => contextualAlternates;
		/// <summary>Gets whether contextual ligatures are enabled.</summary>
		public bool ContextualLigatures => contextualLigatures;
		/// <summary>Gets contextual swashes count.</summary>
		public int ContextualSwashes => contextualSwashes;
		/// <summary>Gets whether discretionary ligatures are enabled.</summary>
		public bool DiscretionaryLigatures => discretionaryLigatures;
		/// <summary>Gets whether East Asian expert forms are enabled.</summary>
		public bool EastAsianExpertForms => eastAsianExpertForms;
		/// <summary>Gets East Asian language setting.</summary>
		public object EastAsianLanguage => eastAsianLanguage;
		/// <summary>Gets East Asian widths setting.</summary>
		public object EastAsianWidths => eastAsianWidths;
		/// <summary>Gets font fraction setting.</summary>
		public object Fraction => fraction;
		/// <summary>Gets whether historical forms are enabled.</summary>
		public bool HistoricalForms => historicalForms;
		/// <summary>Gets whether historical ligatures are enabled.</summary>
		public bool HistoricalLigatures => historicalLigatures;
		/// <summary>Gets whether kerning is enabled.</summary>
		public bool Kerning => kerning;
		/// <summary>Gets whether mathematical Greek is enabled.</summary>
		public bool MathematicalGreek => mathematicalGreek;
		/// <summary>Gets numeral alignment setting.</summary>
		public object NumeralAlignment => numeralAlignment;
		/// <summary>Gets numeral style setting.</summary>
		public object NumeralStyle => numeralStyle;
		/// <summary>Gets whether slashed zero is enabled.</summary>
		public bool SlashedZero => slashedZero;
		/// <summary>Gets whether standard ligatures are enabled.</summary>
		public bool StandardLigatures => standardLigatures;
		/// <summary>Gets standard swashes count.</summary>
		public int StandardSwashes => standardSwashes;
		/// <summary>Gets stylistic alternates count.</summary>
		public int StylisticAlternates => stylisticAlternates;
		/// <summary>Gets font variant setting.</summary>
		public object Variants => variants;
		/// <summary>Gets stylistic set 1.</summary>
		public bool StylisticSet1 => stylisticSets[0];
		/// <summary>Gets stylistic set 2.</summary>
		public bool StylisticSet2 => stylisticSets[1];
		/// <summary>Gets stylistic set 3.</summary>
		public bool StylisticSet3 => stylisticSets[2];
		/// <summary>Gets stylistic set 4.</summary>
		public bool StylisticSet4 => stylisticSets[3];
		/// <summary>Gets stylistic set 5.</summary>
		public bool StylisticSet5 => stylisticSets[4];
		/// <summary>Gets stylistic set 6.</summary>
		public bool StylisticSet6 => stylisticSets[5];
		/// <summary>Gets stylistic set 7.</summary>
		public bool StylisticSet7 => stylisticSets[6];
		/// <summary>Gets stylistic set 8.</summary>
		public bool StylisticSet8 => stylisticSets[7];
		/// <summary>Gets stylistic set 9.</summary>
		public bool StylisticSet9 => stylisticSets[8];
		/// <summary>Gets stylistic set 10.</summary>
		public bool StylisticSet10 => stylisticSets[9];
		/// <summary>Gets stylistic set 11.</summary>
		public bool StylisticSet11 => stylisticSets[10];
		/// <summary>Gets stylistic set 12.</summary>
		public bool StylisticSet12 => stylisticSets[11];
		/// <summary>Gets stylistic set 13.</summary>
		public bool StylisticSet13 => stylisticSets[12];
		/// <summary>Gets stylistic set 14.</summary>
		public bool StylisticSet14 => stylisticSets[13];
		/// <summary>Gets stylistic set 15.</summary>
		public bool StylisticSet15 => stylisticSets[14];
		/// <summary>Gets stylistic set 16.</summary>
		public bool StylisticSet16 => stylisticSets[15];
		/// <summary>Gets stylistic set 17.</summary>
		public bool StylisticSet17 => stylisticSets[16];
		/// <summary>Gets stylistic set 18.</summary>
		public bool StylisticSet18 => stylisticSets[17];
		/// <summary>Gets stylistic set 19.</summary>
		public bool StylisticSet19 => stylisticSets[18];
		/// <summary>Gets stylistic set 20.</summary>
		public bool StylisticSet20 => stylisticSets[19];
	}
}
