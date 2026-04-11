// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// Generates folding elements in the visual line.
	/// </summary>
	public sealed class FoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
	{
		readonly List<TextView> textViews = new List<TextView>();
		FoldingManager foldingManager;

		/// <summary>Gets/sets the FoldingManager providing folding information.</summary>
		public FoldingManager FoldingManager {
			get => foldingManager;
			set {
				if (ReferenceEquals(foldingManager, value))
					return;

				foldingManager = value;
				foreach (var view in textViews)
					view.Redraw();
			}
		}

		/// <summary>Gets/sets the default text brush for folding markers.</summary>
		public static Brush DefaultTextBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);

		/// <summary>Gets/sets the text brush for folding markers.</summary>
		public static Brush TextBrush { get; set; } = DefaultTextBrush;

		/// <inheritdoc/>
		public override void StartGeneration(ITextRunConstructionContext context)
		{
			base.StartGeneration(context);
			if (foldingManager == null)
				return;

			if (!ReferenceEquals(context.Document, foldingManager.Document))
				throw new ArgumentException("Invalid document for this folding manager.", nameof(context));
		}

		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (foldingManager == null)
				return -1;

			foreach (var fs in foldingManager.GetFoldingsContaining(startOffset)) {
				if (fs.IsFolded && fs.EndOffset > startOffset)
					return startOffset;
			}

			return foldingManager.GetNextFoldedFoldingStart(startOffset);
		}

		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset)
		{
			if (foldingManager == null)
				return null;

			var foldedUntil = -1;
			FoldingSection chosen = null;
			foreach (var fs in foldingManager.GetFoldingsContaining(offset)) {
				if (fs.IsFolded && fs.EndOffset > foldedUntil) {
					foldedUntil = fs.EndOffset;
					chosen = fs;
				}
			}

			if (chosen == null || foldedUntil <= offset)
				return null;

			bool foundOverlapping;
			do {
				foundOverlapping = false;
				foreach (var fs in foldingManager.GetFoldingsContaining(foldedUntil).Where(f => f.IsFolded && f.EndOffset > foldedUntil)) {
					foldedUntil = fs.EndOffset;
					foundOverlapping = true;
				}
			} while (foundOverlapping);

			return new FormattedTextElement(foldedUntil - offset);
		}

		void ITextViewConnect.AddToTextView(TextView textView)
		{
			if (textView == null)
				return;
			if (!textViews.Contains(textView))
				textViews.Add(textView);
		}

		void ITextViewConnect.RemoveFromTextView(TextView textView)
		{
			if (textView == null)
				return;
			textViews.Remove(textView);
		}
	}
}
