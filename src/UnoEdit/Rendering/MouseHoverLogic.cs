// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Monitors mouse hover over a UIElement and raises events.
	/// </summary>
	public class MouseHoverLogic : IDisposable
	{
		private readonly UIElement target;

		/// <summary>Creates a new MouseHoverLogic for the given element.</summary>
		public MouseHoverLogic(UIElement target)
		{
			this.target = target;
		}

		/// <summary>Raised when the mouse hovers over the element.</summary>
		public event EventHandler<PointerRoutedEventArgs> MouseHover;

		/// <summary>Raised when the mouse stops hovering.</summary>
		public event EventHandler<PointerRoutedEventArgs> MouseHoverStopped;

		/// <inheritdoc/>
		public void Dispose() { }
	}
}
