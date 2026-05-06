// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — folded-line elements use the lightweight text formatting shim.

using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Folding
{
	public sealed partial class FoldingElementGenerator
	{
		partial void OnFoldingManagerChanged()
		{
			foreach (var view in textViews)
				view.Redraw();
		}

		partial void ValidateTextView(ITextRunConstructionContext context)
		{
			if (!textViews.Contains(context.TextView))
				throw new System.ArgumentException("Invalid TextView");
		}

		private partial VisualLineElement CreateFoldingElement(FoldingSection foldingSection, string title, int documentLength)
		{
			return new FormattedTextElement(documentLength);
		}
	}
}
