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

		public TextView()
		{
			services.AddService(typeof(TextView), this);
			elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
			lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
			backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
		}

		/// <summary>Gets or sets the text document displayed by this view.</summary>
		public TextDocument Document { get; set; }

		/// <summary>Gets or sets the editor options.</summary>
		public TextEditorOptions Options { get; set; } = new TextEditorOptions();

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
	}
}
