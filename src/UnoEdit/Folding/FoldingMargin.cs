// UnoEdit port of ICSharpCode.AvalonEdit.Folding.FoldingMargin.
// FoldingMargin is a margin control that shows folding markers.
// The WPF custom-painting is replaced with Uno's vector graphics.
using Microsoft.UI.Xaml.Media;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A margin that shows markers for code foldings.
	/// </summary>
	public class FoldingMargin
	{
		/// <summary>Gets/Sets the folding manager from which foldings are shown.</summary>
		public ICSharpCode.AvalonEdit.Folding.FoldingManager FoldingManager { get; set; }

		// ----------------------------------------------------------------
		// FoldingMarkerBrush attached property
		// ----------------------------------------------------------------

		/// <summary>FoldingMarkerBrush dependency property.</summary>
		public static readonly DependencyProperty FoldingMarkerBrushProperty =
			DependencyProperty.RegisterAttached(
				"FoldingMarkerBrush",
				typeof(Brush),
				typeof(FoldingMargin),
				new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.Gray)));

		/// <summary>Gets the FoldingMarkerBrush from a DependencyObject.</summary>
		public static Brush GetFoldingMarkerBrush(DependencyObject obj)
			=> (Brush)obj.GetValue(FoldingMarkerBrushProperty);

		/// <summary>Sets the FoldingMarkerBrush on a DependencyObject.</summary>
		public static void SetFoldingMarkerBrush(DependencyObject obj, Brush value)
			=> obj.SetValue(FoldingMarkerBrushProperty, value);

		/// <summary>Gets or sets the brush used for folding marker lines.</summary>
		public Brush FoldingMarkerBrush
		{
			get => _foldingMarkerBrush;
			set => _foldingMarkerBrush = value;
		}
		private Brush _foldingMarkerBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);

		// ----------------------------------------------------------------
		// FoldingMarkerBackgroundBrush attached property
		// ----------------------------------------------------------------

		/// <summary>FoldingMarkerBackgroundBrush dependency property.</summary>
		public static readonly DependencyProperty FoldingMarkerBackgroundBrushProperty =
			DependencyProperty.RegisterAttached(
				"FoldingMarkerBackgroundBrush",
				typeof(Brush),
				typeof(FoldingMargin),
				new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.White)));

		/// <summary>Gets the FoldingMarkerBackgroundBrush from a DependencyObject.</summary>
		public static Brush GetFoldingMarkerBackgroundBrush(DependencyObject obj)
			=> (Brush)obj.GetValue(FoldingMarkerBackgroundBrushProperty);

		/// <summary>Sets the FoldingMarkerBackgroundBrush on a DependencyObject.</summary>
		public static void SetFoldingMarkerBackgroundBrush(DependencyObject obj, Brush value)
			=> obj.SetValue(FoldingMarkerBackgroundBrushProperty, value);

		/// <summary>Gets or sets the background brush for folding markers.</summary>
		public Brush FoldingMarkerBackgroundBrush
		{
			get => _foldingMarkerBackgroundBrush;
			set => _foldingMarkerBackgroundBrush = value;
		}
		private Brush _foldingMarkerBackgroundBrush = new SolidColorBrush(Microsoft.UI.Colors.White);

		// ----------------------------------------------------------------
		// SelectedFoldingMarkerBrush attached property
		// ----------------------------------------------------------------

		/// <summary>SelectedFoldingMarkerBrush dependency property.</summary>
		public static readonly DependencyProperty SelectedFoldingMarkerBrushProperty =
			DependencyProperty.RegisterAttached(
				"SelectedFoldingMarkerBrush",
				typeof(Brush),
				typeof(FoldingMargin),
				new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.Black)));

		/// <summary>Gets the SelectedFoldingMarkerBrush from a DependencyObject.</summary>
		public static Brush GetSelectedFoldingMarkerBrush(DependencyObject obj)
			=> (Brush)obj.GetValue(SelectedFoldingMarkerBrushProperty);

		/// <summary>Sets the SelectedFoldingMarkerBrush on a DependencyObject.</summary>
		public static void SetSelectedFoldingMarkerBrush(DependencyObject obj, Brush value)
			=> obj.SetValue(SelectedFoldingMarkerBrushProperty, value);

		/// <summary>Gets or sets the brush for selected folding markers.</summary>
		public Brush SelectedFoldingMarkerBrush
		{
			get => _selectedFoldingMarkerBrush;
			set => _selectedFoldingMarkerBrush = value;
		}
		private Brush _selectedFoldingMarkerBrush = new SolidColorBrush(Microsoft.UI.Colors.Black);

		// ----------------------------------------------------------------
		// SelectedFoldingMarkerBackgroundBrush attached property
		// ----------------------------------------------------------------

		/// <summary>SelectedFoldingMarkerBackgroundBrush dependency property.</summary>
		public static readonly DependencyProperty SelectedFoldingMarkerBackgroundBrushProperty =
			DependencyProperty.RegisterAttached(
				"SelectedFoldingMarkerBackgroundBrush",
				typeof(Brush),
				typeof(FoldingMargin),
				new PropertyMetadata(new SolidColorBrush(Microsoft.UI.Colors.White)));

		/// <summary>Gets the SelectedFoldingMarkerBackgroundBrush from a DependencyObject.</summary>
		public static Brush GetSelectedFoldingMarkerBackgroundBrush(DependencyObject obj)
			=> (Brush)obj.GetValue(SelectedFoldingMarkerBackgroundBrushProperty);

		/// <summary>Sets the SelectedFoldingMarkerBackgroundBrush on a DependencyObject.</summary>
		public static void SetSelectedFoldingMarkerBackgroundBrush(DependencyObject obj, Brush value)
			=> obj.SetValue(SelectedFoldingMarkerBackgroundBrushProperty, value);

		/// <summary>Gets or sets the background brush for selected folding markers.</summary>
		public Brush SelectedFoldingMarkerBackgroundBrush
		{
			get => _selectedFoldingMarkerBackgroundBrush;
			set => _selectedFoldingMarkerBackgroundBrush = value;
		}
		private Brush _selectedFoldingMarkerBackgroundBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
	}
}
