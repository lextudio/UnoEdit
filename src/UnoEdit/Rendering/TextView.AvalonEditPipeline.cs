using System;
using System.Collections.Generic;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace UnoEdit.Skia.Desktop.Controls
{
    // AvalonEdit rendering-pipeline parity: WPF AvalonEdit's TextView exposes ElementGenerators /
    // BackgroundRenderers / LineTransformers collections plus SetResourceReference / InvalidateLayer,
    // which ILSpy's DecompilerTextView (and its TextMarkerService / bracket / reference renderers)
    // drive. The Skia TextView gains the same surface here. The collections are honored by the
    // colorizing/visual-line pipeline where wired; renderers added here that the Skia draw path
    // does not yet consume are inert (parity is being filled incrementally).
    partial class TextView
    {
        public IList<VisualLineElementGenerator> ElementGenerators { get; } = new List<VisualLineElementGenerator>();

        public IList<IBackgroundRenderer> BackgroundRenderers { get; } = new List<IBackgroundRenderer>();

        public IList<IVisualLineTransformer> LineTransformers { get; } = new List<IVisualLineTransformer>();

        /// <summary>Requests a redraw of the specified layer. Skia repaint is driven elsewhere; no-op for now.</summary>
        public void InvalidateLayer(KnownLayer layer) { }

        /// <summary>WPF FrameworkElement.SetResourceReference parity — resolves a themed resource by key.
        /// Skia theming is applied via the editor theme; this accepts the call so renderers compile.</summary>
        public void SetResourceReference(Microsoft.UI.Xaml.DependencyProperty dependencyProperty, object resourceKey) { }

        // --- Background highlight painting (UnoRichText-style Rectangle overlay) ---
        // Highlights are positioned in content space on BackgroundHighlightCanvas (which scrolls
        // with the text), so GetRectsForSegment's viewport rects are converted back by adding the
        // scroll offsets. Used by bracket-match + reference-marker rendering.

        /// <summary>Removes painted background highlights belonging to the given layer key
        /// (e.g. "bracket", "marker") so independent highlight sets can coexist.</summary>
        public void ClearBackgroundHighlights(string key)
        {
            if (BackgroundHighlightCanvas is null)
                return;
            for (int i = BackgroundHighlightCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (BackgroundHighlightCanvas.Children[i] is Microsoft.UI.Xaml.FrameworkElement fe && Equals(fe.Tag, key))
                    BackgroundHighlightCanvas.Children.RemoveAt(i);
            }
        }

        /// <summary>Paints a filled/outlined highlight over a (single-line) document segment,
        /// positioned in content space via GetVisualPosition. Tagged with the layer key.</summary>
        public void AddBackgroundHighlight(string key, ISegment segment, Microsoft.UI.Xaml.Media.Brush? fill, Microsoft.UI.Xaml.Media.Brush? stroke)
        {
            if (BackgroundHighlightCanvas is null || segment is null || Document is null)
                return;

            var startLoc = Document.GetLocation(segment.Offset);
            var endLoc = Document.GetLocation(segment.EndOffset);
            var p0 = GetVisualPosition(new ICSharpCode.AvalonEdit.TextViewPosition(startLoc.Line, startLoc.Column), VisualYPosition.LineTop);
            var p1 = GetVisualPosition(new ICSharpCode.AvalonEdit.TextViewPosition(endLoc.Line, endLoc.Column), VisualYPosition.LineTop);
            double width = startLoc.Line == endLoc.Line ? Math.Max(2.0, p1.X - p0.X) : Math.Max(2.0, EditorFontSize);

            var rect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = width,
                Height = LineHeight,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = stroke is null ? 0 : 1,
                RadiusX = 2,
                RadiusY = 2,
                Tag = key,
            };
            Microsoft.UI.Xaml.Controls.Canvas.SetLeft(rect, p0.X);
            Microsoft.UI.Xaml.Controls.Canvas.SetTop(rect, p0.Y);
            BackgroundHighlightCanvas.Children.Add(rect);
        }
    }
}
