// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Shared Uno-side host for rendering services and extension points.
	/// The actual interactive control lives in the Uno XAML layer, but the shared rendering
	/// pipeline still expects a text view object that can host generators, transformers,
	/// background renderers, and service lookups.
	/// </summary>
	public sealed partial class TextView
	{
		readonly HashSet<KnownLayer> invalidatedLayers = new HashSet<KnownLayer>();
		readonly List<CollapsedLineSection> collapsedLineSections = new List<CollapsedLineSection>();
		HeightTree heightTree;

		// Cached helpers for prepared formatted text/textline objects.
		internal TextViewCachedElements cachedElements;

		/// <summary>Occurs when the document property changes.</summary>
		public event EventHandler DocumentChanged;

		/// <summary>Occurs when the current visual-line set changes.</summary>
		public event EventHandler VisualLinesChanged;

		/// <summary>Raises the <see cref="DocumentChanged"/> event.</summary>
		void OnDocumentChanged(EventArgs e)
		{
			DocumentChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Resets the shared AvalonEdit visual-line and height-tree caches for a newly attached document.
		/// Invoked from the visual control's AttachDocument.
		/// </summary>
		void ResetVisualLineCacheForDocument(TextDocument newDocument)
		{
			heightTree?.Dispose();
			heightTree = newDocument != null ? new HeightTree(newDocument, DefaultLineHeight > 0 ? DefaultLineHeight : 1.0) : null;
			collapsedLineSections.Clear();
			VisualLinesValid = false;
			VisualLines = new ReadOnlyCollection<VisualLine>(new List<VisualLine>());
			DocumentHeight = 0;
		}

		/// <summary>
		/// Invalidates the current visual-line cache and notifies listeners that the shared host
		/// needs a redraw. The concrete Uno XAML view is still responsible for repainting.
		/// </summary>
		public void Redraw()
		{
			VisualLinesValid = false;
			OnVisualLinesChanged(EventArgs.Empty);
			OnRedrawRequested(EventArgs.Empty);
		}

		/// <summary>Raises the <see cref="VisualLinesChanged"/> event.</summary>
		void OnVisualLinesChanged(EventArgs e)
		{
			VisualLinesChanged?.Invoke(this, e);
		}

		/// <summary>Raised when the shared host needs a redraw.</summary>
		partial void OnRedrawRequested(EventArgs e);

		/// <summary>Raised when a specific rendering layer was invalidated.</summary>
		partial void OnLayerInvalidated(KnownLayer knownLayer);

		// ----------------------------------------------------------------
		// Brush / Pen dependency properties (surface stubs)
		// ----------------------------------------------------------------
		public static readonly object NonPrintableCharacterBrushProperty = null;
		public Brush NonPrintableCharacterBrush { get; set; }

		public static readonly object LinkTextForegroundBrushProperty = null;
		public Brush LinkTextForegroundBrush { get; set; }

		public static readonly object LinkTextBackgroundBrushProperty = null;
		public Brush LinkTextBackgroundBrush { get; set; }

		public static readonly object LinkTextUnderlineProperty = null;
		public bool LinkTextUnderline { get; set; } = true;

		public static readonly object ColumnRulerPenProperty = null;
		public System.Windows.Media.Pen ColumnRulerPen { get; set; }

		public static readonly object CurrentLineBackgroundProperty = null;
		public Brush CurrentLineBackground { get; set; }

		public static readonly object CurrentLineBorderProperty = null;
		public System.Windows.Media.Pen CurrentLineBorder { get; set; }

		// ----------------------------------------------------------------
		// Layout metrics (shared defaults overridden by the concrete Uno XAML control when available)
		// ----------------------------------------------------------------
		public double DocumentHeight {
			get {
				if (!VisualLinesValid)
					EnsureVisualLines();
				return documentHeight;
			}
			internal set { documentHeight = value; }
		}
		public double DefaultLineHeight {
			get { return defaultLineHeight; }
			internal set {
				defaultLineHeight = value;
				if (heightTree != null)
					heightTree.DefaultLineHeight = value > 0 ? value : 1.0;
			}
		}
		public double DefaultBaseline { get; internal set; }
		public double WideSpaceWidth { get; internal set; }
		public double EmptyLineSelectionWidth { get; set; } = 1.0;
		double documentHeight;
		double defaultLineHeight;

		// ----------------------------------------------------------------
		// Scroll state
		// ----------------------------------------------------------------
		// NOTE: These are stored (non-visual) offsets retained so BackgroundGeometryBuilder and other
		// shared AvalonEdit code compile. The visual rendering reads TextScrollViewer directly and does
		// NOT update these; ScrollOffset (the live offset) is provided by the visual partial.
		public double HorizontalOffset { get; internal set; }
		public double VerticalOffset { get; internal set; }

		// ----------------------------------------------------------------
		// Visual lines
		// ----------------------------------------------------------------
		public ReadOnlyCollection<VisualLine> VisualLines { get; internal set; } = new ReadOnlyCollection<VisualLine>(new List<VisualLine>());
		public bool VisualLinesValid { get; internal set; }
		public int HighlightedLine { get; set; }
		internal IReadOnlyCollection<KnownLayer> InvalidatedLayers => invalidatedLayers;

		/// <summary>Ensures a minimal shared visual-line model exists for the current document.</summary>
		public void EnsureVisualLines()
		{
			if (VisualLinesValid)
				return;

			BuildSimpleVisualLines();
			VisualLinesValid = true;
			OnVisualLinesChanged(EventArgs.Empty);
		}

		/// <summary>Returns the visual line for the given document line number, or null.</summary>
		public VisualLine GetVisualLine(int documentLineNumber)
		{
			foreach (var vl in VisualLines)
				if (vl.FirstDocumentLine?.LineNumber == documentLineNumber) return vl;
			return null;
		}

		/// <summary>Returns or constructs the visual line for the given document line.</summary>
		public VisualLine GetOrConstructVisualLine(DocumentLine documentLine)
		{
			if (documentLine == null)
				return null;

			VisualLine line = GetVisualLine(documentLine.LineNumber);
			if (line != null)
				return line;

			EnsureVisualLines();
			return GetVisualLine(documentLine.LineNumber);
		}

		/// <summary>Returns the document line at the given visual top position.</summary>
		public DocumentLine GetDocumentLineByVisualTop(double visualTop)
		{
			EnsureVisualLines();
			var visualLine = GetVisualLineFromVisualTop(visualTop);
			if (visualLine != null)
				return visualLine.FirstDocumentLine;

			if (Document == null || Document.LineCount == 0)
				return null;

			if (DefaultLineHeight > 0) {
				int lineNumber = Math.Max(1, Math.Min(Document.LineCount, (int)(visualTop / DefaultLineHeight) + 1));
				return Document.GetLineByNumber(lineNumber);
			}

			return Document.GetLineByNumber(1);
		}

		/// <summary>Returns the visual line at the given visual top position.</summary>
		public VisualLine GetVisualLineFromVisualTop(double visualTop)
		{
			EnsureVisualLines();

			VisualLine lastLine = null;
			foreach (var visualLine in VisualLines) {
				lastLine = visualLine;
				double top = visualLine.VisualTop;
				double bottom = top + Math.Max(visualLine.Height, DefaultLineHeight);
				if (visualTop >= top && visualTop < bottom)
					return visualLine;
			}

			if (lastLine != null && visualTop >= lastLine.VisualTop)
				return lastLine;

			return null;
		}

		/// <summary>Returns the visual top position for the given document line number.</summary>
		public double GetVisualTopByDocumentLine(int line)
		{
			EnsureVisualLines();
			var visualLine = GetVisualLine(line);
			if (visualLine != null)
				return visualLine.VisualTop;

			return line > 1 && DefaultLineHeight > 0 ? (line - 1) * DefaultLineHeight : 0.0;
		}

		/// <summary>Gets the text view position (floor) from a visual position.</summary>
		public TextViewPosition? GetPositionFloor(Point visualPosition)
		{
			if (Document == null)
				return null;

			EnsureVisualLines();
			var visualLine = GetVisualLineFromVisualTop(visualPosition.Y);
			if (visualLine == null)
				return null;

			return visualLine.GetTextViewPositionFloor(visualPosition, Options.EnableVirtualSpace);
		}

		/// <summary>Gets the visual position from a text view position.</summary>
		public Point GetVisualPosition(TextViewPosition position, VisualYPosition yPositionMode)
		{
			EnsureVisualLines();
			var visualLine = GetVisualLine(position.Line);
			if (visualLine == null)
				return new Point(0, GetVisualTopByDocumentLine(position.Line));

			int visualColumn = position.VisualColumn >= 0 ? position.VisualColumn : Math.Max(0, position.Column - 1);
			return visualLine.GetVisualPosition(visualColumn, yPositionMode);
		}

		void BuildSimpleVisualLines()
		{
			// Capture the inline objects from the previous build before rebuilding. The rebuild
			// below re-attaches only the elements present in the current document; afterwards we
			// detach any that are gone. Without this, embedded controls (e.g. a resource ListView)
			// linger in the host visual tree after the user navigates to another node.
			var host = Document?.ServiceProvider.GetService(typeof(IInlineObjectHost)) as IInlineObjectHost;
			if (host != null)
				inlineObjectHost = host;
			var previousInlineObjects = new List<InlineObjectRun>(inlineObjects);
			inlineObjects.Clear();

			if (Document == null || Document.LineCount == 0) {
				VisualLines = new ReadOnlyCollection<VisualLine>(new List<VisualLine>());
				DocumentHeight = 0;
				DetachStaleInlineElements(previousInlineObjects);
				return;
			}

			double lineHeight = DefaultLineHeight > 0 ? DefaultLineHeight : 1.0;
			var lines = new List<VisualLine>(Document.LineCount);
			DocumentLine current = Document.GetLineByNumber(1);
			double visualTop = 0;
			var generators = elementGenerators?.ToArray() ?? Array.Empty<VisualLineElementGenerator>();
			var transformers = lineTransformers?.ToArray() ?? Array.Empty<IVisualLineTransformer>();
			while (current != null) {
				var visualLine = new VisualLine(this, current) {
					LastDocumentLine = current,
					VisualTop = visualTop,
					Height = lineHeight,
					VisualLength = current.Length
				};
				if (generators.Length > 0 || transformers.Length > 0) {
					var context = new SimpleTextRunConstructionContext(this, visualLine);
					visualLine.ConstructVisualElements(context, generators);
					if (transformers.Length > 0)
						visualLine.RunTransformers(context, transformers);
					AttachInlineObjectsForVisualLine(visualLine, visualTop, lineHeight);
				} else {
					visualLine.Elements = new ReadOnlyCollection<VisualLineElement>(new List<VisualLineElement>());
				}
				lines.Add(visualLine);
				visualTop += Math.Max(lineHeight, visualLine.Height);
				current = current.NextLine;
			}

			VisualLines = new ReadOnlyCollection<VisualLine>(lines);
			DocumentHeight = visualTop;
			DetachStaleInlineElements(previousInlineObjects);
		}

		// Detach inline elements that were attached in a previous build but are no longer part of
		// the current set of inline objects, removing them from the host visual tree.
		void DetachStaleInlineElements(List<InlineObjectRun> previousInlineObjects)
		{
			if (previousInlineObjects.Count == 0 || inlineObjectHost == null)
				return;

			foreach (var previous in previousInlineObjects) {
				bool stillPresent = false;
				for (int i = 0; i < inlineObjects.Count; i++) {
					if (inlineObjects[i].Element == previous.Element) {
						stillPresent = true;
						break;
					}
				}
				if (!stillPresent)
					inlineObjectHost.DetachInlineElement(previous.Element);
			}
		}

		void AttachInlineObjectsForVisualLine(VisualLine visualLine, double visualTop, double lineHeight)
		{
			if (visualLine?.Elements == null)
				return;

			foreach (var element in visualLine.Elements) {
				if (element is not InlineObjectElement inlineElement)
					continue;

				var run = (InlineObjectRun)inlineElement.CreateTextRun(element.VisualColumn, new SimpleTextRunConstructionContext(this, visualLine));
				AddInlineObject(run);

				double x = 0;
				double width = Math.Max(1.0, run.desiredMetrics.Size.Width);
				double height = Math.Max(1.0, run.desiredMetrics.Size.Height);
				bool isButton = run.Element?.GetType().Name.Contains("Button", StringComparison.Ordinal) == true;
				if (isButton)
					height = Math.Min(height, 24);
				double lineRelativeTop = isButton || height <= lineHeight * 2 ? lineHeight : 0;
				double y = visualTop + lineRelativeTop;
				visualLine.hasInlineObjects = true;
				visualLine.Height = Math.Max(visualLine.Height, lineRelativeTop + height);
				(Document?.ServiceProvider.GetService(typeof(IInlineObjectHost)) as IInlineObjectHost)
					?.ArrangeInlineElement(run.Element, new Windows.Foundation.Rect(x, y, width, height));
			}
		}

		sealed class SimpleTextRunConstructionContext : ITextRunConstructionContext
		{
			readonly TextView textView;
			readonly VisualLine visualLine;

			public SimpleTextRunConstructionContext(TextView textView, VisualLine visualLine)
			{
				this.textView = textView;
				this.visualLine = visualLine;
				GlobalTextRunProperties = new VisualLineElementTextRunProperties();
			}

			public TextDocument Document => textView.Document;
			public TextView TextView => textView;
			public VisualLine VisualLine => visualLine;
			public System.Windows.Media.TextFormatting.TextRunProperties GlobalTextRunProperties { get; }

			public StringSegment GetText(int offset, int length)
			{
				if (Document == null || length <= 0)
					return new StringSegment(string.Empty, 0, 0);

				int safeOffset = Math.Max(0, Math.Min(offset, Document.TextLength));
				int safeLength = Math.Max(0, Math.Min(length, Document.TextLength - safeOffset));
				return new StringSegment(Document.GetText(safeOffset, safeLength), 0, safeLength);
			}
		}

		// ----------------------------------------------------------------
		// Layer management
		// ----------------------------------------------------------------
		public IList<UIElement> Layers { get; } = new List<UIElement>();

		/// <summary>Inserts a layer at the given position.</summary>
		public void InsertLayer(UIElement layer, KnownLayer referencedLayer, LayerInsertionPosition position)
		{
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			if (!Enum.IsDefined(typeof(KnownLayer), referencedLayer))
				throw new InvalidEnumArgumentException(nameof(referencedLayer), (int)referencedLayer, typeof(KnownLayer));
			if (!Enum.IsDefined(typeof(LayerInsertionPosition), position))
				throw new InvalidEnumArgumentException(nameof(position), (int)position, typeof(LayerInsertionPosition));
			if (referencedLayer == KnownLayer.Background && position != LayerInsertionPosition.Above)
				throw new InvalidOperationException("Cannot replace or insert below the background layer.");

			LayerPosition.SetLayerPosition(layer, new LayerPosition(referencedLayer, position));

			for (int i = 0; i < Layers.Count; i++) {
				LayerPosition p = LayerPosition.GetLayerPosition(Layers[i]);
				if (p == null)
					continue;

				if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Replace) {
					switch (position) {
						case LayerInsertionPosition.Below:
							Layers.Insert(i, layer);
							return;
						case LayerInsertionPosition.Above:
							Layers.Insert(i + 1, layer);
							return;
						case LayerInsertionPosition.Replace:
							Layers[i] = layer;
							return;
					}
				}

				if ((p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Above)
					|| p.KnownLayer > referencedLayer) {
					Layers.Insert(i, layer);
					return;
				}
			}

			Layers.Add(layer);
		}

		// ----------------------------------------------------------------
		// Inline object handling (host-side surface support)
		// ----------------------------------------------------------------
		List<InlineObjectRun> inlineObjects = new List<InlineObjectRun>();

		// Last known inline-object host (the platform TextView). Cached so stale elements can be
		// detached even on a rebuild where the document (and thus its ServiceProvider) is gone.
		IInlineObjectHost inlineObjectHost;

		/// <summary>
		/// Adds a new inline object. Concrete UI layer may implement HostAddVisual to attach the element.
		/// </summary>
		internal void AddInlineObject(InlineObjectRun inlineObject)
		{
			if (inlineObject == null) throw new ArgumentNullException(nameof(inlineObject));

			// Remove inline object if its already added (recreation scenarios)
			bool alreadyAdded = false;
			for (int i = 0; i < inlineObjects.Count; i++) {
				if (inlineObjects[i].Element == inlineObject.Element) {
					RemoveInlineObjectRun(inlineObjects[i], true);
					inlineObjects.RemoveAt(i);
					alreadyAdded = true;
					break;
				}
			}

			inlineObjects.Add(inlineObject);
				var host = Document?.ServiceProvider.GetService(typeof(IInlineObjectHost)) as IInlineObjectHost;
				if (!alreadyAdded && host != null) {
					host.AttachInlineElement(inlineObject.Element);
				}
				if (host != null) {
					inlineObject.desiredMetrics = host.MeasureInlineElement(inlineObject.Element);
				}
		}

		List<VisualLine> visualLinesWithOutstandingInlineObjects = new List<VisualLine>();

		internal void RemoveInlineObjects(VisualLine visualLine)
		{
			if (visualLine == null) return;
			if (visualLine.hasInlineObjects) {
				visualLinesWithOutstandingInlineObjects.Add(visualLine);
			}
		}

		/// <summary>
		/// Remove the inline objects that were marked for removal.
		/// </summary>
		internal void RemoveInlineObjectsNow()
		{
			if (visualLinesWithOutstandingInlineObjects.Count == 0)
				return;
			inlineObjects.RemoveAll(
				ior => {
					if (visualLinesWithOutstandingInlineObjects.Contains(ior.VisualLine)) {
						RemoveInlineObjectRun(ior, false);
						return true;
					}
					return false;
				});
			visualLinesWithOutstandingInlineObjects.Clear();
		}

		// Caller of RemoveInlineObjectRun will remove it from inlineObjects collection.
		void RemoveInlineObjectRun(InlineObjectRun ior, bool keepElement)
		{
			if (ior == null) return;
			ior.VisualLine = null;
			if (!keepElement) {
				var host = Document?.ServiceProvider.GetService(typeof(IInlineObjectHost)) as IInlineObjectHost;
				if (host != null) {
					host.DetachInlineElement(ior.Element);
				}
			}
		}



		/// <summary>Collapses lines between the start and end document lines using the shared height-tree backend.</summary>
		public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end)
		{
			if (Document == null)
				throw new InvalidOperationException("Cannot collapse lines without a document.");
			if (start == null)
				throw new ArgumentNullException(nameof(start));
			if (end == null)
				throw new ArgumentNullException(nameof(end));
			if (start.LineNumber > end.LineNumber)
				throw new ArgumentException("The start line must not come after the end line.");

			heightTree ??= new HeightTree(Document, DefaultLineHeight > 0 ? DefaultLineHeight : 1.0);
			CollapsedLineSection section = heightTree.CollapseText(start, end);
			collapsedLineSections.Add(section);
			Redraw();
			return section;
		}

		/// <summary>Raised when shared code requests a cursor redraw.</summary>
		public static event EventHandler CursorInvalidated;

		internal static int CursorInvalidationVersion { get; private set; }

		/// <summary>Invalidates the cursor and notifies active hosts that caret visuals need repainting.</summary>
		public static void InvalidateCursor()
		{
			CursorInvalidationVersion++;
			CursorInvalidated?.Invoke(null, EventArgs.Empty);
		}

		/// <summary>Makes the given rectangle visible by adjusting the stored scroll offsets.</summary>
		public void MakeVisible(Rect rectangle)
		{
			double newHorizontal = HorizontalOffset;
			double newVertical = VerticalOffset;

			if (rectangle.Left < newHorizontal)
				newHorizontal = rectangle.Left;
			if (rectangle.Top < newVertical)
				newVertical = rectangle.Top;

			newHorizontal = Math.Max(0, newHorizontal);
			newVertical = Math.Max(0, newVertical);

			if (!newHorizontal.Equals(HorizontalOffset) || !newVertical.Equals(VerticalOffset)) {
				HorizontalOffset = newHorizontal;
				VerticalOffset = newVertical;
				ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		// ----------------------------------------------------------------
		// Mouse hover events
		// ----------------------------------------------------------------
		public static readonly object MouseHoverEvent = null;
		public event EventHandler MouseHover;
		public static readonly object MouseHoverStoppedEvent = null;
		public event EventHandler MouseHoverStopped;
		public static readonly object PreviewMouseHoverEvent = null;
		public event EventHandler PreviewMouseHover;
		public static readonly object PreviewMouseHoverStoppedEvent = null;
		public event EventHandler PreviewMouseHoverStopped;

		// ----------------------------------------------------------------
		// Option changed / visual line construction events
		// ----------------------------------------------------------------
		public event PropertyChangedEventHandler OptionChanged;
		public event EventHandler<VisualLineConstructionStartEventArgs> VisualLineConstructionStarting;

		/// <summary>Raises <see cref="OptionChanged"/> for the given property.</summary>
		void OnOptionChanged(PropertyChangedEventArgs e)
		{
			OptionChanged?.Invoke(this, e);
		}

		// Helpers to suppress unused-event warnings
		void RaiseMouseHover(EventArgs e) { MouseHover?.Invoke(this, e); }
		void RaiseMouseHoverStopped(EventArgs e) { MouseHoverStopped?.Invoke(this, e); }
		void RaisePreviewMouseHover(EventArgs e) { PreviewMouseHover?.Invoke(this, e); }
		void RaisePreviewMouseHoverStopped(EventArgs e) { PreviewMouseHoverStopped?.Invoke(this, e); }
		void RaiseVisualLineConstructionStarting(VisualLineConstructionStartEventArgs e) { VisualLineConstructionStarting?.Invoke(this, e); }
	}
}
