using System;
using System.Collections.Generic;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
    // AvalonEdit rendering-pipeline parity: WPF AvalonEdit's TextView exposes ElementGenerators /
    // BackgroundRenderers / LineTransformers collections plus SetResourceReference / InvalidateLayer,
    // which ILSpy's DecompilerTextView (and its TextMarkerService / bracket / reference renderers)
    // drive. The Skia TextView gains the same surface here. The collections are honored by the
    // colorizing/visual-line pipeline where wired; renderers added here that the Skia draw path
    // does not yet consume are inert (parity is being filled incrementally).
    partial class TextView
    {
        // Pipeline collections with ITextViewConnect add/remove wiring (ported from the former
        // headless TextView). Backing fields are initialized in InitializePipelineCollections(),
        // invoked from the primary constructor in TextView.xaml.uno.cs.
        ObserveAddRemoveCollection<VisualLineElementGenerator> elementGenerators;
        ObserveAddRemoveCollection<IVisualLineTransformer> lineTransformers;
        ObserveAddRemoveCollection<IBackgroundRenderer> backgroundRenderers;

        void InitializePipelineCollections()
        {
            elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
            lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
            backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
        }

        /// <summary>Gets a collection where element generators can be registered.</summary>
        public IList<VisualLineElementGenerator> ElementGenerators => elementGenerators;

        /// <summary>Gets the list of background renderers.</summary>
        public IList<IBackgroundRenderer> BackgroundRenderers => backgroundRenderers;

        /// <summary>Gets a collection where line transformers can be registered.</summary>
        public IList<IVisualLineTransformer> LineTransformers => lineTransformers;

        void ElementGenerator_Added(VisualLineElementGenerator generator)
        {
            ConnectToTextView(generator);
            Redraw();
        }

        void ElementGenerator_Removed(VisualLineElementGenerator generator)
        {
            DisconnectFromTextView(generator);
            Redraw();
        }

        void LineTransformer_Added(IVisualLineTransformer lineTransformer)
        {
            ConnectToTextView(lineTransformer);
            Redraw();
        }

        void LineTransformer_Removed(IVisualLineTransformer lineTransformer)
        {
            DisconnectFromTextView(lineTransformer);
            Redraw();
        }

        void BackgroundRenderer_Added(IBackgroundRenderer renderer)
        {
            ConnectToTextView(renderer);
            InvalidateLayer(renderer.Layer);
        }

        void BackgroundRenderer_Removed(IBackgroundRenderer renderer)
        {
            DisconnectFromTextView(renderer);
            InvalidateLayer(renderer.Layer);
        }

        void ConnectToTextView(object obj)
        {
            if (obj is ITextViewConnect connectable)
                connectable.AddToTextView(this);
        }

        void DisconnectFromTextView(object obj)
        {
            if (obj is ITextViewConnect connectable)
                connectable.RemoveFromTextView(this);
        }

        /// <summary>Requests a redraw of the specified layer. Skia repaint is driven elsewhere.</summary>
        public void InvalidateLayer(KnownLayer layer)
        {
            invalidatedLayers.Add(layer);
            OnLayerInvalidated(layer);
        }

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

        /// <summary>Scroll offset of the editor viewport, matching WPF AvalonEdit's TextView.ScrollOffset.</summary>
        public Windows.Foundation.Point ScrollOffset
            => new Windows.Foundation.Point(TextScrollViewer.HorizontalOffset, TextScrollViewer.VerticalOffset);

        /// <summary>Converts a content-space visual position to a TextViewPosition (line/column).
        /// Mirrors WPF AvalonEdit's TextView.GetPosition(Point), enabling reference-navigation hit-testing.</summary>
        public ICSharpCode.AvalonEdit.TextViewPosition? GetPosition(Windows.Foundation.Point contentPosition)
        {
            if (Document is null || Document.LineCount == 0)
                return null;
            int line = Math.Clamp((int)(contentPosition.Y / LineHeight) + 1, 1, Document.LineCount);
            // Real glyph-advance hit-testing via the shared measurement (replaces the fontSize*0.6
            // monospace guess) so column resolution matches the rendered glyph positions.
            string lineText = Document.GetText(Document.GetLineByNumber(line));
            int logicalColumn = GetLogicalColumnFromDisplayX(lineText, contentPosition.X);
            int column = Math.Clamp(logicalColumn + 1, 1, lineText.Length + 1);
            return new ICSharpCode.AvalonEdit.TextViewPosition(line, column);
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
