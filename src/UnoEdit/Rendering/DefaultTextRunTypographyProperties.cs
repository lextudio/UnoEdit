// Stub for DefaultTextRunTypographyProperties — replaces WPF TextRunTypographyProperties.
// All properties return default values.
namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Stub implementation of DefaultTextRunTypographyProperties.
	/// Returns default values for all typography properties.
	/// </summary>
	public class DefaultTextRunTypographyProperties
	{
		public enum TypographyKind
		{
			Default,
			Normal,
			Proportional
		}

		/// <summary>Gets annotation alternates count.</summary>
		public int AnnotationAlternates => 0;
		/// <summary>Gets font capitals setting.</summary>
		public object Capitals => TypographyKind.Default;
		/// <summary>Gets whether capital spacing is enabled.</summary>
		public bool CapitalSpacing => false;
		/// <summary>Gets whether case-sensitive forms are enabled.</summary>
		public bool CaseSensitiveForms => false;
		/// <summary>Gets whether contextual alternates are enabled.</summary>
		public bool ContextualAlternates => true;
		/// <summary>Gets whether contextual ligatures are enabled.</summary>
		public bool ContextualLigatures => true;
		/// <summary>Gets contextual swashes count.</summary>
		public int ContextualSwashes => 0;
		/// <summary>Gets whether discretionary ligatures are enabled.</summary>
		public bool DiscretionaryLigatures => false;
		/// <summary>Gets whether East Asian expert forms are enabled.</summary>
		public bool EastAsianExpertForms => false;
		/// <summary>Gets East Asian language setting.</summary>
		public object EastAsianLanguage => TypographyKind.Default;
		/// <summary>Gets East Asian widths setting.</summary>
		public object EastAsianWidths => TypographyKind.Normal;
		/// <summary>Gets font fraction setting.</summary>
		public object Fraction => TypographyKind.Default;
		/// <summary>Gets whether historical forms are enabled.</summary>
		public bool HistoricalForms => false;
		/// <summary>Gets whether historical ligatures are enabled.</summary>
		public bool HistoricalLigatures => false;
		/// <summary>Gets whether kerning is enabled.</summary>
		public bool Kerning => true;
		/// <summary>Gets whether mathematical Greek is enabled.</summary>
		public bool MathematicalGreek => false;
		/// <summary>Gets numeral alignment setting.</summary>
		public object NumeralAlignment => TypographyKind.Proportional;
		/// <summary>Gets numeral style setting.</summary>
		public object NumeralStyle => TypographyKind.Normal;
		/// <summary>Gets whether slashed zero is enabled.</summary>
		public bool SlashedZero => false;
		/// <summary>Gets whether standard ligatures are enabled.</summary>
		public bool StandardLigatures => true;
		/// <summary>Gets standard swashes count.</summary>
		public int StandardSwashes => 0;
		/// <summary>Gets stylistic alternates count.</summary>
		public int StylisticAlternates => 0;
		/// <summary>Gets font variant setting.</summary>
		public object Variants => TypographyKind.Normal;
		/// <summary>Gets stylistic set 1.</summary>
		public bool StylisticSet1 => false;
		/// <summary>Gets stylistic set 2.</summary>
		public bool StylisticSet2 => false;
		/// <summary>Gets stylistic set 3.</summary>
		public bool StylisticSet3 => false;
		/// <summary>Gets stylistic set 4.</summary>
		public bool StylisticSet4 => false;
		/// <summary>Gets stylistic set 5.</summary>
		public bool StylisticSet5 => false;
		/// <summary>Gets stylistic set 6.</summary>
		public bool StylisticSet6 => false;
		/// <summary>Gets stylistic set 7.</summary>
		public bool StylisticSet7 => false;
		/// <summary>Gets stylistic set 8.</summary>
		public bool StylisticSet8 => false;
		/// <summary>Gets stylistic set 9.</summary>
		public bool StylisticSet9 => false;
		/// <summary>Gets stylistic set 10.</summary>
		public bool StylisticSet10 => false;
		/// <summary>Gets stylistic set 11.</summary>
		public bool StylisticSet11 => false;
		/// <summary>Gets stylistic set 12.</summary>
		public bool StylisticSet12 => false;
		/// <summary>Gets stylistic set 13.</summary>
		public bool StylisticSet13 => false;
		/// <summary>Gets stylistic set 14.</summary>
		public bool StylisticSet14 => false;
		/// <summary>Gets stylistic set 15.</summary>
		public bool StylisticSet15 => false;
		/// <summary>Gets stylistic set 16.</summary>
		public bool StylisticSet16 => false;
		/// <summary>Gets stylistic set 17.</summary>
		public bool StylisticSet17 => false;
		/// <summary>Gets stylistic set 18.</summary>
		public bool StylisticSet18 => false;
		/// <summary>Gets stylistic set 19.</summary>
		public bool StylisticSet19 => false;
		/// <summary>Gets stylistic set 20.</summary>
		public bool StylisticSet20 => false;
	}
}
