// Portable subset of ICSharpCode.AvalonEdit.Utils.ExtensionMethods.
// Contains only methods with no WPF dependencies.
// WPF-specific methods (TransformToDevice, VisualAncestorsAndSelf, CreateTypeface, etc.)
// are omitted; they are not needed by the files linked in UnoEdit.

using System;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Utils
{
	public static partial class ExtensionMethods
	{
		/// <summary>Epsilon used for IsClose() implementations.</summary>
		public const double Epsilon = 1e-10;

		public static bool IsClose(this double d1, double d2)
		{
			if (d1 == d2)
				return true;
			return Math.Abs(d1 - d2) < Epsilon;
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
	}
}
