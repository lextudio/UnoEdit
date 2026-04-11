// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Builds a geometry for drawing backgrounds behind segments.
	/// </summary>
	public sealed class BackgroundGeometryBuilder
	{
		/// <summary>Creates a new BackgroundGeometryBuilder.</summary>
		public BackgroundGeometryBuilder() { }

		/// <summary>Gets/sets the corner radius for rounded rectangles.</summary>
		public double CornerRadius { get; set; }

		/// <summary>Gets/sets whether to align to whole pixels.</summary>
		public bool AlignToWholePixels { get; set; }

		/// <summary>Gets/sets the border thickness.</summary>
		public double BorderThickness { get; set; }

		/// <summary>Gets/sets whether to extend to full width at line end.</summary>
		public bool ExtendToFullWidthAtLineEnd { get; set; }

		/// <summary>Adds a document segment to the geometry.</summary>
		public void AddSegment(TextView textView, ISegment segment) { }

		/// <summary>Adds a rectangle to the geometry.</summary>
		public void AddRectangle(TextView textView, Rect rectangle) { }

		/// <summary>Adds a rectangle by coordinates.</summary>
		public void AddRectangle(double left, double top, double right, double bottom) { }

		/// <summary>Closes the current figure.</summary>
		public void CloseFigure() { }

		/// <summary>Creates the final geometry.</summary>
		public object CreateGeometry() => null;

		/// <summary>Gets rectangles for a document segment.</summary>
		public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
			=> Array.Empty<Rect>();

		/// <summary>Gets rectangles from a visual segment.</summary>
		public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVC, int endVC)
			=> Array.Empty<Rect>();
	}
}
