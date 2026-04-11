// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using Microsoft.UI.Xaml.Media;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// Generates folding elements in the visual line.
	/// </summary>
	public sealed class FoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
	{
		/// <summary>Gets/sets the FoldingManager providing folding information.</summary>
		public FoldingManager FoldingManager { get; set; }

		/// <summary>Gets/sets the default text brush for folding markers.</summary>
		public static Brush DefaultTextBrush { get; set; }

		/// <summary>Gets/sets the text brush for folding markers.</summary>
		public static Brush TextBrush { get; set; }

		/// <inheritdoc/>
		public override void StartGeneration(ITextRunConstructionContext context) { }

		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset) => -1;

		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset) => null;

		void ITextViewConnect.AddToTextView(TextView textView) { }
		void ITextViewConnect.RemoveFromTextView(TextView textView) { }
	}
}
