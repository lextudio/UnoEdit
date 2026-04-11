// Uno-native implementation of the code-completion list box.
// Extends ListView (the WinUI equivalent of ListBox) and adds helpers
// for scrolling, selection, and visible-item counting.
using Microsoft.UI.Xaml.Controls;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// The list box used inside the <see cref="CompletionList"/>.
	/// Extends <see cref="ListView"/> with AvalonEdit-specific helpers.
	/// </summary>
	public class CompletionListBox : ListView
	{
		internal ScrollViewer scrollViewer;

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			scrollViewer = null;
		}

		/// <summary>Parity shim overload keeping OnApplyTemplate in the public API surface.</summary>
		public void OnApplyTemplate(object _)
		{
			base.OnApplyTemplate();
		}

		/// <summary>
		/// Gets or sets the index of the first visible item.
		/// </summary>
		public int FirstVisibleItem
		{
			get
			{
				if (scrollViewer == null || Items.Count == 0) return 0;
				return (int)(Items.Count * scrollViewer.VerticalOffset
							 / System.Math.Max(1, scrollViewer.ExtentHeight));
			}
			set
			{
				value = System.Math.Max(0, System.Math.Min(value, Items.Count - VisibleItemCount));
				if (scrollViewer != null)
					scrollViewer.ScrollToVerticalOffset(
						(double)value / System.Math.Max(1, Items.Count)
						* scrollViewer.ExtentHeight);
			}
		}

		/// <summary>Gets the number of visible items.</summary>
		public int VisibleItemCount
		{
			get
			{
				if (scrollViewer == null || scrollViewer.ExtentHeight == 0) return 10;
				return System.Math.Max(3,
					(int)System.Math.Ceiling(Items.Count
						* scrollViewer.ViewportHeight
						/ scrollViewer.ExtentHeight));
			}
		}

		/// <summary>Removes the selection.</summary>
		public void ClearSelection() => SelectedIndex = -1;

		/// <summary>Selects the item at <paramref name="index"/> and scrolls it into view.</summary>
		public void SelectIndex(int index)
		{
			if (index >= Items.Count) index = Items.Count - 1;
			if (index < 0) index = 0;
			SelectedIndex = index;
			ScrollIntoView(Items[index]);
		}

		/// <summary>Centers the view on the item at <paramref name="index"/>.</summary>
		public void CenterViewOn(int index) => FirstVisibleItem = index - VisibleItemCount / 2;
	}
}
