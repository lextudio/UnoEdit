using System.IO;
using System.Linq;
using System.Xml;

using NUnit.Framework;

using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace ICSharpCode.AvalonEdit.Utils
{
	[TestFixture]
	public class ExtensionMethodsTests
	{
		[Test]
		public void IsClose_UsesPixelFriendlyEpsilon()
		{
			Assert.That(1.0.IsClose(1.009), Is.True);
			Assert.That(1.0.IsClose(1.02), Is.False);
		}

		[Test]
		public void IsClose_SizeAndVector_WorkAsExpected()
		{
			Assert.That(new Size(10, 20).IsClose(new Size(10.009, 20.009)), Is.True);
			Assert.That(new System.Windows.Vector(4, 5).IsClose(new System.Windows.Vector(4.005, 5.005)), Is.True);
			Assert.That(new System.Windows.Vector(4, 5).IsClose(new System.Windows.Vector(4.02, 5)), Is.False);
		}

		[Test]
		public void XmlHelpers_ReadAttributesAndBooleans()
		{
			var document = new XmlDocument();
			document.LoadXml("<node enabled=\"true\" name=\"demo\" />");

			Assert.That(document.DocumentElement.GetAttributeOrNull("name"), Is.EqualTo("demo"));
			Assert.That(document.DocumentElement.GetBoolAttribute("enabled"), Is.EqualTo(true));

			using var reader = XmlReader.Create(new StringReader("<node enabled=\"false\" />"));
			Assert.That(reader.Read(), Is.True);
			Assert.That(reader.GetBoolAttribute("enabled"), Is.EqualTo(false));
		}

		[Test]
		public void DeviceTransformHelpers_ApplyMatrixScalingAndOffsets()
		{
			var visual = new System.Windows.Media.Visual
			{
				PresentationSource = new System.Windows.PresentationSource
				{
					CompositionTarget = new System.Windows.Media.CompositionTarget
					{
						TransformToDevice = new System.Windows.Media.Matrix
						{
							M11 = 2,
							M22 = 3,
							OffsetX = 4,
							OffsetY = 5
						},
						TransformFromDevice = new System.Windows.Media.Matrix
						{
							M11 = 0.5,
							M22 = 1.0 / 3.0,
							OffsetX = -2,
							OffsetY = -1
						}
					}
				}
			};

			Assert.That(new Point(1, 2).TransformToDevice(visual), Is.EqualTo(new Point(6, 11)));
			Assert.That(new Size(7, 8).TransformToDevice(visual), Is.EqualTo(new Size(14, 24)));
			Assert.That(new Rect(1, 2, 3, 4).TransformToDevice(visual), Is.EqualTo(new Rect(6, 11, 6, 12)));
			Assert.That(new Point(6, 11).TransformFromDevice(visual), Is.EqualTo(new Point(1, 8.0 / 3.0)));

			var rect = new Rect(6, 11, 6, 12).TransformFromDevice(visual);
			Assert.That(rect.X, Is.EqualTo(1).Within(0.001));
			Assert.That(rect.Y, Is.EqualTo(8.0 / 3.0).Within(0.001));
			Assert.That(rect.Width, Is.EqualTo(3).Within(0.001));
			Assert.That(rect.Height, Is.EqualTo(4).Within(0.001));
		}

		[Test]
		public void DrawingConversions_RoundTripCoordinates()
		{
			var point = new Point(12.9, 4.2);
			var drawingPoint = point.ToSystemDrawing();
			var rect = new System.Drawing.Rectangle(drawingPoint, new System.Drawing.Size(8, 9));

			Assert.That(drawingPoint, Is.EqualTo(new System.Drawing.Point(12, 4)));
			Assert.That(drawingPoint.ToWpf(), Is.EqualTo(new Point(12, 4)));
			Assert.That(rect.ToWpf(), Is.EqualTo(new Rect(12, 4, 8, 9)));
			Assert.That(ExtensionMethods.Sequence("only").Single(), Is.EqualTo("only"));
		}
	}
}
