using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	internal static class CompletionWindowPlacement
	{
		private const double Margin = 4d;

		public static Point Place(Rect caretRect, Size popupSize, XamlRoot? xamlRoot)
		{
			Size? viewportSize = xamlRoot is null ? null : xamlRoot.Size;
			return Place(caretRect, popupSize, viewportSize);
		}

		public static Point Place(Rect caretRect, Size popupSize, Size? viewportSize)
		{
			double viewportWidth = viewportSize?.Width ?? double.PositiveInfinity;
			double viewportHeight = viewportSize?.Height ?? double.PositiveInfinity;

			double popupWidth = popupSize.Width;
			double popupHeight = popupSize.Height;

			double x = caretRect.X;
			double belowY = caretRect.Y + caretRect.Height + Margin;
			double aboveY = caretRect.Y - popupHeight - Margin;

			if (!double.IsInfinity(viewportWidth) && popupWidth > 0)
			{
				x = Math.Clamp(x, 0d, Math.Max(0d, viewportWidth - popupWidth));
			}

			double y = belowY;
			if (!double.IsInfinity(viewportHeight) && popupHeight > 0)
			{
				bool fitsBelow = belowY + popupHeight <= viewportHeight;
				bool fitsAbove = aboveY >= 0d;

				if (!fitsBelow && fitsAbove)
				{
					y = aboveY;
				}
				else
				{
					y = Math.Clamp(belowY, 0d, Math.Max(0d, viewportHeight - popupHeight));
				}
			}

			return new Point(x, y);
		}
	}
}
