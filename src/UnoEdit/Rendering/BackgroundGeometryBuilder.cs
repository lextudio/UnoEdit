// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Builds a geometry for drawing backgrounds behind segments.
	/// </summary>
	public sealed class BackgroundGeometryBuilder
	{
		readonly List<Rect> rectangles = new List<Rect>();
		readonly List<int> closedFigureOffsets = new List<int>();

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
		public void AddSegment(TextView textView, ISegment segment)
		{
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (segment == null)
				throw new ArgumentNullException(nameof(segment));

			foreach (var rect in GetRectsForSegment(textView, segment, ExtendToFullWidthAtLineEnd))
				AddRectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
		}

		/// <summary>Adds a rectangle to the geometry.</summary>
		public void AddRectangle(TextView textView, Rect rectangle)
		{
			AddRectangle(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		}

		/// <summary>Adds a rectangle by coordinates.</summary>
		public void AddRectangle(double left, double top, double right, double bottom)
		{
			if (right < left) {
				var t = left;
				left = right;
				right = t;
			}
			if (bottom < top) {
				var t = top;
				top = bottom;
				bottom = t;
			}

			if (AlignToWholePixels) {
				left = Math.Round(left, MidpointRounding.AwayFromZero);
				right = Math.Round(right, MidpointRounding.AwayFromZero);
				top = Math.Round(top, MidpointRounding.AwayFromZero);
				bottom = Math.Round(bottom, MidpointRounding.AwayFromZero);
			}

			if (right <= left || bottom <= top)
				return;

			rectangles.Add(new Rect(left, top, right - left, bottom - top));
		}

		/// <summary>Closes the current figure so later rectangles start a new grouping boundary.</summary>
		public void CloseFigure()
		{
			int rectangleCount = rectangles.Count;
			if (rectangleCount == 0)
				return;

			if (closedFigureOffsets.Count == 0 || closedFigureOffsets[closedFigureOffsets.Count - 1] != rectangleCount)
				closedFigureOffsets.Add(rectangleCount);
		}

		internal IReadOnlyList<int> ClosedFigureOffsets => closedFigureOffsets;

		/// <summary>Creates the final geometry.</summary>
		public object CreateGeometry()
		{
			if (rectangles.Count == 0)
				return null;

			var group = new GeometryGroup();
			foreach (var rect in rectangles) {
				group.Children.Add(new RectangleGeometry {
					Rect = rect
				});
			}
			return group;
		}

		/// <summary>Gets rectangles for a document segment.</summary>
		public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
		{
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (segment == null)
				throw new ArgumentNullException(nameof(segment));
			if (textView.Document == null)
				return Array.Empty<Rect>();

			var doc = textView.Document;
			var start = Math.Clamp(segment.Offset, 0, doc.TextLength);
			var end = Math.Clamp(segment.EndOffset, start, doc.TextLength);
			if (end <= start)
				return Array.Empty<Rect>();

			var lineHeight = textView.DefaultLineHeight > 0 ? textView.DefaultLineHeight : 16.0;
			const double charWidth = 7.0;
			var rects = new List<Rect>();

			var line = doc.GetLineByOffset(start);
			while (line != null && line.Offset <= end) {
				var lineStart = Math.Max(start, line.Offset);
				var lineEnd = Math.Min(end, line.EndOffset);
				var colStart = Math.Max(0, lineStart - line.Offset);
				var colEnd = Math.Max(colStart, lineEnd - line.Offset);

				var y = textView.GetVisualTopByDocumentLine(line.LineNumber) - textView.VerticalOffset;
				var x = colStart * charWidth - textView.HorizontalOffset;
				var width = extendToFullWidthAtLineEnd ? 4096.0 : Math.Max(1.0, (colEnd - colStart) * charWidth);
				rects.Add(new Rect(x, y, width, lineHeight));

				line = line.NextLine;
			}

			return rects;
		}

		/// <summary>Gets rectangles from a visual segment.</summary>
		public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVC, int endVC)
		{
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (line == null)
				throw new ArgumentNullException(nameof(line));

			var lineHeight = textView.DefaultLineHeight > 0 ? textView.DefaultLineHeight : 16.0;
			const double charWidth = 7.0;
			var x = startVC * charWidth - textView.HorizontalOffset;
			var width = Math.Max(1.0, (endVC - startVC) * charWidth);
			var y = textView.GetVisualTopByDocumentLine(line.FirstDocumentLine.LineNumber) - textView.VerticalOffset;
			return new[] { new Rect(x, y, width, lineHeight) };
		}
	}
}
