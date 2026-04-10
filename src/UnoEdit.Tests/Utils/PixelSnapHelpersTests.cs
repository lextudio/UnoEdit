using NUnit.Framework;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;
using Point = Windows.Foundation.Point;

namespace ICSharpCode.AvalonEdit.Utils
{
    [TestFixture]
    public class PixelSnapHelpersTests
    {
        [Test]
        public void GetPixelSize_WithoutPresentationSource_ReturnsUnitSize()
        {
            var visual = new System.Windows.Media.Visual();

            var pixelSize = PixelSnapHelpers.GetPixelSize(visual);

            Assert.That(pixelSize.Width, Is.EqualTo(1));
            Assert.That(pixelSize.Height, Is.EqualTo(1));
        }

        [Test]
        public void GetPixelSize_UsesPresentationSourceTransform()
        {
            var visual = new System.Windows.Media.Visual
            {
                PresentationSource = new System.Windows.PresentationSource
                {
                    CompositionTarget = new System.Windows.Media.CompositionTarget
                    {
                        TransformFromDevice = new System.Windows.Media.Matrix { M11 = 0.5, M22 = 0.75 }
                    }
                }
            };

            var pixelSize = PixelSnapHelpers.GetPixelSize(visual);

            Assert.That(pixelSize.Width, Is.EqualTo(0.5));
            Assert.That(pixelSize.Height, Is.EqualTo(0.75));
        }

        [Test]
        public void PixelAlign_RoundsToPixelCenter()
        {
            Assert.That(PixelSnapHelpers.PixelAlign(1.0, 1.0), Is.EqualTo(1.5));
            Assert.That(PixelSnapHelpers.PixelAlign(0.1, 1.0), Is.EqualTo(0.5));
        }

        [Test]
        public void Round_RectAndPoint_UsePixelGrid()
        {
            var roundedPoint = PixelSnapHelpers.Round(new Point(1.24, 2.76), new Size(0.5, 0.5));
            var roundedRect = PixelSnapHelpers.Round(new Rect(0.74, 1.26, 2.24, 3.76), new Size(0.5, 0.5));

            Assert.That(roundedPoint.X, Is.EqualTo(1.0));
            Assert.That(roundedPoint.Y, Is.EqualTo(3.0));
            Assert.That(roundedRect.X, Is.EqualTo(0.5));
            Assert.That(roundedRect.Y, Is.EqualTo(1.5));
            Assert.That(roundedRect.Width, Is.EqualTo(2.0));
            Assert.That(roundedRect.Height, Is.EqualTo(4.0));
        }
    }
}
