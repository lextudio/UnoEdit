// Stub for ICSharpCode.AvalonEdit.Folding.FoldingMargin.
// FoldingMargin is a margin control that shows folding markers.
// This stub provides the API surface without WPF-specific rendering.
namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A margin that shows markers for code foldings.
	/// </summary>
	public class FoldingMargin
	{
		/// <summary>Gets/Sets the folding manager from which foldings are shown.</summary>
		public FoldingManager FoldingManager { get; set; }

		/// <summary>FoldingMarkerBrush dependency property key.</summary>
		public static readonly object FoldingMarkerBrushProperty = null;

		/// <summary>Gets or sets the brush used for folding marker lines.</summary>
		public Brush FoldingMarkerBrush { get; set; }

		/// <summary>Gets the FoldingMarkerBrush from a DependencyObject.</summary>
		public static Brush GetFoldingMarkerBrush(object obj) => null;

		/// <summary>Sets the FoldingMarkerBrush on a DependencyObject.</summary>
		public static void SetFoldingMarkerBrush(object obj, Brush value) { }

		/// <summary>FoldingMarkerBackgroundBrush dependency property key.</summary>
		public static readonly object FoldingMarkerBackgroundBrushProperty = null;

		/// <summary>Gets or sets the background brush for folding markers.</summary>
		public Brush FoldingMarkerBackgroundBrush { get; set; }

		/// <summary>Gets the FoldingMarkerBackgroundBrush from a DependencyObject.</summary>
		public static Brush GetFoldingMarkerBackgroundBrush(object obj) => null;

		/// <summary>Sets the FoldingMarkerBackgroundBrush on a DependencyObject.</summary>
		public static void SetFoldingMarkerBackgroundBrush(object obj, Brush value) { }

		/// <summary>SelectedFoldingMarkerBrush dependency property key.</summary>
		public static readonly object SelectedFoldingMarkerBrushProperty = null;

		/// <summary>Gets or sets the brush for selected folding markers.</summary>
		public Brush SelectedFoldingMarkerBrush { get; set; }

		/// <summary>Gets the SelectedFoldingMarkerBrush from a DependencyObject.</summary>
		public static Brush GetSelectedFoldingMarkerBrush(object obj) => null;

		/// <summary>Sets the SelectedFoldingMarkerBrush on a DependencyObject.</summary>
		public static void SetSelectedFoldingMarkerBrush(object obj, Brush value) { }

		/// <summary>SelectedFoldingMarkerBackgroundBrush dependency property key.</summary>
		public static readonly object SelectedFoldingMarkerBackgroundBrushProperty = null;

		/// <summary>Gets or sets the background brush for selected folding markers.</summary>
		public Brush SelectedFoldingMarkerBackgroundBrush { get; set; }

		/// <summary>Gets the SelectedFoldingMarkerBackgroundBrush from a DependencyObject.</summary>
		public static Brush GetSelectedFoldingMarkerBackgroundBrush(object obj) => null;

		/// <summary>Sets the SelectedFoldingMarkerBackgroundBrush on a DependencyObject.</summary>
		public static void SetSelectedFoldingMarkerBackgroundBrush(object obj, Brush value) { }
	}
}
