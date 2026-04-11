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
		/// Placeholder for the WPF redraw entry point.
		/// The Uno XAML control performs actual rendering invalidation separately.
		/// </summary>
		public virtual void Redraw()
		{
		}

		/// <summary>
		/// Placeholder for layer invalidation in the Uno host.
		/// </summary>
		public virtual void InvalidateLayer(KnownLayer knownLayer)
		{
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
		// Layout metrics (read-only stubs; actual values come from the Uno XAML control)
		// ----------------------------------------------------------------
		public double DocumentHeight { get; internal set; }
		public double DefaultLineHeight { get; internal set; }
		public double DefaultBaseline { get; internal set; }
		public double WideSpaceWidth { get; internal set; }
		public double EmptyLineSelectionWidth { get; set; } = 1.0;

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

		/// <summary>Ensures visual lines are up to date; no-op in this stub.</summary>
		public void EnsureVisualLines() { }

		/// <summary>Returns the visual line for the given document line number, or null.</summary>
		public VisualLine GetVisualLine(int documentLineNumber)
		{
			foreach (var vl in VisualLines)
				if (vl.FirstDocumentLine?.LineNumber == documentLineNumber) return vl;
			return null;
		}

		/// <summary>Returns or constructs the visual line for the given document line; stub returns null.</summary>
		public VisualLine GetOrConstructVisualLine(DocumentLine documentLine) => GetVisualLine(documentLine?.LineNumber ?? 0);

		/// <summary>Returns the document line at the given visual top position; stub.</summary>
		public DocumentLine GetDocumentLineByVisualTop(double visualTop) => null;

		/// <summary>Returns the visual line at the given visual top position; stub.</summary>
		public VisualLine GetVisualLineFromVisualTop(double visualTop) => null;

		/// <summary>Returns the visual top position for the given document line number; stub.</summary>
		public double GetVisualTopByDocumentLine(int line) => 0.0;

		/// <summary>Gets the text view position from a visual position; stub.</summary>
		public TextViewPosition? GetPosition(Point visualPosition) => null;

		/// <summary>Gets the text view position (floor) from a visual position; stub.</summary>
		public TextViewPosition? GetPositionFloor(Point visualPosition) => null;

		/// <summary>Gets the visual position from a text view position; stub.</summary>
		public Point GetVisualPosition(TextViewPosition position, VisualYPosition yPositionMode) => default;

		// ----------------------------------------------------------------
		// Layer management
		// ----------------------------------------------------------------
		public IList<UIElement> Layers { get; } = new List<UIElement>();

		/// <summary>Inserts a layer at the given position; no-op in this stub.</summary>
		public void InsertLayer(UIElement layer, KnownLayer referencedLayer, LayerInsertionPosition position) { }

		/// <summary>Collapses lines between the start and end document lines; stub.</summary>
		public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end) => null;

		/// <summary>Invalidates the cursor; no-op in this stub.</summary>
		public static void InvalidateCursor() { }

		/// <summary>Makes the given rectangle visible; no-op in this stub.</summary>
		public virtual void MakeVisible(Rect rectangle) { }

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
