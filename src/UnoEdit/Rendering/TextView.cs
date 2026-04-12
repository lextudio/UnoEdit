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
	public class TextView
	{
		readonly ObserveAddRemoveCollection<VisualLineElementGenerator> elementGenerators;
		readonly ObserveAddRemoveCollection<IVisualLineTransformer> lineTransformers;
		readonly ObserveAddRemoveCollection<IBackgroundRenderer> backgroundRenderers;
		readonly ServiceContainer services = new ServiceContainer();
		readonly HashSet<KnownLayer> invalidatedLayers = new HashSet<KnownLayer>();
		TextDocument document;

		public TextView()
		{
			services.AddService(typeof(TextView), this);
			elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
			lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
			backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
		}

		/// <summary>Gets or sets the text document displayed by this view.</summary>
		public TextDocument Document {
			get { return document; }
			set {
				if (!ReferenceEquals(document, value)) {
					document = value;
					VisualLinesValid = false;
					VisualLines = new ReadOnlyCollection<VisualLine>(new List<VisualLine>());
					DocumentHeight = 0;
					OnDocumentChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Gets or sets the editor options.</summary>
		public TextEditorOptions Options { get; set; } = new TextEditorOptions();

		/// <summary>Occurs when the document property changes.</summary>
		public event EventHandler DocumentChanged;

		/// <summary>Occurs when the current visual-line set changes.</summary>
		public event EventHandler VisualLinesChanged;

		/// <summary>Occurs when the text view scroll offset changes.</summary>
		public event EventHandler ScrollOffsetChanged;

		/// <summary>Gets a collection where element generators can be registered.</summary>
		public IList<VisualLineElementGenerator> ElementGenerators {
			get { return elementGenerators; }
		}

		/// <summary>Gets a collection where line transformers can be registered.</summary>
		public IList<IVisualLineTransformer> LineTransformers {
			get { return lineTransformers; }
		}

		/// <summary>Gets the list of background renderers.</summary>
		public IList<IBackgroundRenderer> BackgroundRenderers {
			get { return backgroundRenderers; }
		}

		/// <summary>Gets a service container used to associate services with the text view.</summary>
		public IServiceContainer Services {
			get { return services; }
		}

		/// <summary>
		/// Looks up a service on the text view and falls back to the current document's services.
		/// </summary>
		public virtual object GetService(Type serviceType)
		{
			if (serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));

			object instance = services.GetService(serviceType);
			if (instance == null && Document != null)
				instance = Document.ServiceProvider.GetService(serviceType);
			return instance;
		}

		/// <summary>
		/// Invalidates the current visual-line cache and notifies listeners that the shared host
		/// needs a redraw. The concrete Uno XAML view is still responsible for repainting.
		/// </summary>
		public virtual void Redraw()
		{
			VisualLinesValid = false;
			OnVisualLinesChanged(EventArgs.Empty);
			OnRedrawRequested(EventArgs.Empty);
		}

		/// <summary>
		/// Invalidates a rendering layer in the shared host so background renderers and other
		/// shared components can request a repaint without knowing the Uno control details.
		/// </summary>
		public virtual void InvalidateLayer(KnownLayer knownLayer)
		{
			if (!Enum.IsDefined(typeof(KnownLayer), knownLayer))
				throw new InvalidEnumArgumentException(nameof(knownLayer), (int)knownLayer, typeof(KnownLayer));

			invalidatedLayers.Add(knownLayer);
			OnLayerInvalidated(knownLayer);
		}

		/// <summary>Raises the <see cref="DocumentChanged"/> event.</summary>
		protected virtual void OnDocumentChanged(EventArgs e)
		{
			DocumentChanged?.Invoke(this, e);
		}

		/// <summary>Raises the <see cref="VisualLinesChanged"/> event.</summary>
		protected virtual void OnVisualLinesChanged(EventArgs e)
		{
			VisualLinesChanged?.Invoke(this, e);
		}

		/// <summary>Raises the <see cref="ScrollOffsetChanged"/> event.</summary>
		protected virtual void OnScrollOffsetChanged(EventArgs e)
		{
			ScrollOffsetChanged?.Invoke(this, e);
		}

		/// <summary>Raised when the shared host needs a redraw.</summary>
		protected virtual void OnRedrawRequested(EventArgs e)
		{
		}

		/// <summary>Raised when a specific rendering layer was invalidated.</summary>
		protected virtual void OnLayerInvalidated(KnownLayer knownLayer)
		{
		}

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
		public double DefaultLineHeight { get; internal set; }
		public double DefaultBaseline { get; internal set; }
		public double WideSpaceWidth { get; internal set; }
		public double EmptyLineSelectionWidth { get; set; } = 1.0;
		double documentHeight;

		// ----------------------------------------------------------------
		// Scroll state
		// ----------------------------------------------------------------
		public double HorizontalOffset { get; internal set; }
		public double VerticalOffset { get; internal set; }
		/// <summary>Gets the horizontal and vertical scroll offset (combined).</summary>
		public Point ScrollOffset => new Point(HorizontalOffset, VerticalOffset);

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

		/// <summary>Gets the text view position from a visual position.</summary>
		public TextViewPosition? GetPosition(Point visualPosition)
		{
			return GetPositionFloor(visualPosition);
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
			if (Document == null || Document.LineCount == 0) {
				VisualLines = new ReadOnlyCollection<VisualLine>(new List<VisualLine>());
				DocumentHeight = 0;
				return;
			}

			double lineHeight = DefaultLineHeight > 0 ? DefaultLineHeight : 1.0;
			var lines = new List<VisualLine>(Document.LineCount);
			DocumentLine current = Document.GetLineByNumber(1);
			double visualTop = 0;
			while (current != null) {
				var visualLine = new VisualLine(this, current) {
					LastDocumentLine = current,
					VisualTop = visualTop,
					Height = lineHeight,
					VisualLength = current.Length
				};
				visualLine.Elements = new ReadOnlyCollection<VisualLineElement>(new List<VisualLineElement>());
				lines.Add(visualLine);
				visualTop += lineHeight;
				current = current.NextLine;
			}

			VisualLines = new ReadOnlyCollection<VisualLine>(lines);
			DocumentHeight = lines.Count * lineHeight;
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

		/// <summary>Collapses lines between the start and end document lines; stub.</summary>
		public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end) => null;

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
		public virtual void MakeVisible(Rect rectangle)
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
				OnScrollOffsetChanged(EventArgs.Empty);
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
		protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
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
