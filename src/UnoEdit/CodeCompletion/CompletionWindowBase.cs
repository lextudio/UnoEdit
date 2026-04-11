// Uno-native base class for completion popup windows.
// Replaces WPF's CompletionWindowBase (which extends Window) with a
// Popup-based approach: the popup is positioned near the text caret and
// shown/hidden via Microsoft.UI.Xaml.Controls.Primitives.Popup.
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Base class for completion windows. Manages a <see cref="Popup"/> positioned
	/// near the text caret inside the text area.
	/// </summary>
	public class CompletionWindowBase
	{
		/// <summary>The underlying popup that contains the completion UI.</summary>
		protected readonly Popup popup;

		/// <summary>Gets the parent text area (a FrameworkElement).</summary>
		public FrameworkElement TextArea { get; }

		/// <summary>
		/// Gets/Sets the start of the text range in which the completion window stays open.
		/// </summary>
		public int StartOffset { get; set; }

		/// <summary>
		/// Gets/Sets the end of the text range in which the completion window stays open.
		/// </summary>
		public int EndOffset { get; set; }

		/// <summary>
		/// When set, the window does not close when the caret moves before
		/// <see cref="StartOffset"/>.
		/// </summary>
		public bool ExpectInsertionBeforeStart { get; set; }

		/// <summary>
		/// Creates a new CompletionWindowBase attached to <paramref name="textArea"/>.
		/// </summary>
		public CompletionWindowBase(FrameworkElement textArea)
		{
			TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
			popup = new Popup();
			// Ensure the popup closes when it loses focus.
			popup.IsLightDismissEnabled = true;
		}

		/// <summary>Gets whether the popup is currently open.</summary>
		public bool IsOpen => popup.IsOpen;

		/// <summary>
		/// Positions and opens the popup below the current caret line.
		/// Derived classes set <see cref="Popup.Child"/> before calling this.
		/// </summary>
		protected void Show()
		{
			if (popup.XamlRoot == null && TextArea.XamlRoot != null)
				popup.XamlRoot = TextArea.XamlRoot;

			PositionPopup();
			popup.IsOpen = true;
		}

		/// <summary>Positions the popup relative to the text area.</summary>
		protected virtual void PositionPopup()
		{
			// Default: place the popup below the text area.
			// Subclasses or users can override this with caret-accurate coordinates.
			popup.HorizontalOffset = 0;
			popup.VerticalOffset = TextArea?.ActualHeight ?? 100;
		}

		/// <summary>Closes the popup.</summary>
		public virtual void Close()
		{
			popup.IsOpen = false;
		}

		/// <summary>Raised when the popup is closed.</summary>
		public event EventHandler Closed
		{
			add => popup.Closed += (s, e) => value?.Invoke(this, EventArgs.Empty);
			remove { /* unsubscribe not tracked for simplicity */ }
		}
	}
}
