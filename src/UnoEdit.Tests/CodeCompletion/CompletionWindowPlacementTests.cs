using ICSharpCode.AvalonEdit.CodeCompletion;
using NUnit.Framework;
using Windows.Foundation;

namespace UnoEdit.Tests.CodeCompletion
{
	[TestFixture]
	public class CompletionWindowPlacementTests
	{
		[Test]
		public void Place_PrefersBelowCaret_WhenThereIsRoom()
		{
			var caretRect = new Rect(120, 80, 2, 16);
			var popupSize = new Size(175, 100);

			Point result = CompletionWindowPlacement.Place(caretRect, popupSize, viewportSize: null);

			Assert.That(result.X, Is.EqualTo(120).Within(0.001));
			Assert.That(result.Y, Is.EqualTo(100).Within(0.001));
		}

		[Test]
		public void Place_MovesAboveCaret_WhenBelowWouldOverflow()
		{
			var caretRect = new Rect(80, 180, 2, 16);
			var popupSize = new Size(150, 80);

			Point result = CompletionWindowPlacement.Place(caretRect, popupSize, new Size(200, 220));

			Assert.That(result.X, Is.EqualTo(50).Within(0.001));
			Assert.That(result.Y, Is.EqualTo(96).Within(0.001));
		}

		[Test]
		public void Place_ClampsWithinViewport_WhenPopupWouldOverflowHorizontally()
		{
			var caretRect = new Rect(190, 40, 2, 16);
			var popupSize = new Size(120, 90);

			Point result = CompletionWindowPlacement.Place(caretRect, popupSize, new Size(240, 300));

			Assert.That(result.X, Is.EqualTo(120).Within(0.001));
			Assert.That(result.Y, Is.EqualTo(60).Within(0.001));
		}
	}
}
