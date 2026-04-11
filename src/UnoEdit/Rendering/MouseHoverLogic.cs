// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Monitors mouse hover over a UIElement and raises events.
	/// </summary>
	public class MouseHoverLogic : IDisposable
	{
		private readonly UIElement target;
		private readonly DispatcherQueueTimer hoverTimer;
		private PointerRoutedEventArgs lastEventArgs;
		private Point hoverStartPoint;
		private bool isHovering;
		private bool disposed;

		const double HoverTolerance = 4.0;
		static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(400);

		/// <summary>Creates a new MouseHoverLogic for the given element.</summary>
		public MouseHoverLogic(UIElement target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			this.target = target;

			hoverTimer = this.target.DispatcherQueue?.CreateTimer();
			if (hoverTimer != null) {
				hoverTimer.Interval = HoverDelay;
				hoverTimer.IsRepeating = false;
				hoverTimer.Tick += HoverTimerTick;
			}

			this.target.PointerEntered += TargetPointerEntered;
			this.target.PointerMoved += TargetPointerMoved;
			this.target.PointerExited += TargetPointerExited;
			this.target.PointerCanceled += TargetPointerExited;
		}

		/// <summary>Raised when the mouse hovers over the element.</summary>
		public event EventHandler<PointerRoutedEventArgs> MouseHover;

		/// <summary>Raised when the mouse stops hovering.</summary>
		public event EventHandler<PointerRoutedEventArgs> MouseHoverStopped;

		/// <inheritdoc/>
		public void Dispose()
		{
			if (disposed)
				return;

			StopHovering();
			if (hoverTimer != null)
				hoverTimer.Tick -= HoverTimerTick;

			target.PointerEntered -= TargetPointerEntered;
			target.PointerMoved -= TargetPointerMoved;
			target.PointerExited -= TargetPointerExited;
			target.PointerCanceled -= TargetPointerExited;

			disposed = true;
		}

		private void TargetPointerEntered(object sender, PointerRoutedEventArgs e)
		{
			StartHovering(e);
		}

		private void TargetPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			var current = e.GetCurrentPoint(target).Position;
			var dx = Math.Abs(current.X - hoverStartPoint.X);
			var dy = Math.Abs(current.Y - hoverStartPoint.Y);
			if (dx > HoverTolerance || dy > HoverTolerance)
				StartHovering(e);
		}

		private void TargetPointerExited(object sender, PointerRoutedEventArgs e)
		{
			StopHovering();
		}

		private void StartHovering(PointerRoutedEventArgs e)
		{
			StopHovering();
			lastEventArgs = e;
			hoverStartPoint = e.GetCurrentPoint(target).Position;
			hoverTimer?.Start();
		}

		private void StopHovering()
		{
			hoverTimer?.Stop();
			if (isHovering) {
				isHovering = false;
				MouseHoverStopped?.Invoke(this, lastEventArgs);
			}
		}

		private void HoverTimerTick(DispatcherQueueTimer sender, object args)
		{
			sender.Stop();
			isHovering = true;
			MouseHover?.Invoke(this, lastEventArgs);
		}
	}
}
