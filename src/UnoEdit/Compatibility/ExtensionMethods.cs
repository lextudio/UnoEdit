// Portable UnoEdit-maintained subset of ICSharpCode.AvalonEdit.Utils.ExtensionMethods.
// It intentionally keeps non-tree helpers and DPI/math/xml helpers that remain useful in
// the shared desktop-first codebase, while leaving out WPF visual-tree/freezable helpers.

using System;
using System.Collections.Generic;
using System.Xml;

namespace ICSharpCode.AvalonEdit.Utils
{
	public static partial class ExtensionMethods
	{
		public static void CheckIsFrozen(object freezable) { }

		/// <summary>Epsilon used for IsClose() implementations.</summary>
		public const double Epsilon = 0.01;

		public static bool IsClose(this double d1, double d2)
		{
			if (d1 == d2)
				return true;
			return Math.Abs(d1 - d2) < Epsilon;
		}

		public static bool IsClose(this Size d1, Size d2)
		{
			return IsClose(d1.Width, d2.Width) && IsClose(d1.Height, d2.Height);
		}

		public static bool IsClose(this System.Windows.Vector d1, System.Windows.Vector d2)
		{
			return IsClose(d1.X, d2.X) && IsClose(d1.Y, d2.Y);
		}

		public static double CoerceValue(this double value, double minimum, double maximum)
		{
			if (value < minimum)
				return minimum;
			if (value > maximum)
				return maximum;
			return value;
		}

		public static int CoerceValue(this int value, int minimum, int maximum)
		{
			if (value < minimum)
				return minimum;
			if (value > maximum)
				return maximum;
			return value;
		}

		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
		{
			foreach (T element in elements)
				collection.Add(element);
		}

		public static IEnumerable<T> Sequence<T>(T value)
		{
			yield return value;
		}

		public static string GetAttributeOrNull(this XmlElement element, string attributeName)
		{
			XmlAttribute attr = element.GetAttributeNode(attributeName);
			return attr != null ? attr.Value : null;
		}

		public static bool? GetBoolAttribute(this XmlElement element, string attributeName)
		{
			XmlAttribute attr = element.GetAttributeNode(attributeName);
			return attr != null ? (bool?)XmlConvert.ToBoolean(attr.Value) : null;
		}

		public static Rect TransformToDevice(this Rect rect, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return Transform(rect, matrix);
		}

		public static Rect TransformFromDevice(this Rect rect, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformFromDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return Transform(rect, matrix);
		}

		public static Size TransformToDevice(this Size size, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}

		public static Size TransformFromDevice(this Size size, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformFromDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}

		public static Point TransformToDevice(this Point point, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformToDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return Transform(point, matrix);
		}

		public static Point TransformFromDevice(this Point point, System.Windows.Media.Visual visual)
		{
			var matrix = System.Windows.PresentationSource.FromVisual(visual)?.CompositionTarget?.TransformFromDevice
			             ?? System.Windows.Media.Matrix.Identity;
			return Transform(point, matrix);
		}

		public static System.Drawing.Point ToSystemDrawing(this Point p)
		{
			return new System.Drawing.Point((int)p.X, (int)p.Y);
		}

		public static Point ToWpf(this System.Drawing.Point p)
		{
			return new Point(p.X, p.Y);
		}

		public static Size ToWpf(this System.Drawing.Size s)
		{
			return new Size(s.Width, s.Height);
		}

		public static Rect ToWpf(this System.Drawing.Rectangle rect)
		{
			return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
		}

		static Point Transform(Point point, System.Windows.Media.Matrix matrix)
		{
			return new Point(
				point.X * matrix.M11 + matrix.OffsetX,
				point.Y * matrix.M22 + matrix.OffsetY);
		}

		static Rect Transform(Rect rect, System.Windows.Media.Matrix matrix)
		{
			Point topLeft = Transform(new Point(rect.X, rect.Y), matrix);
			Point bottomRight = Transform(new Point(rect.X + rect.Width, rect.Y + rect.Height), matrix);
			return new Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
		}
	}
}
