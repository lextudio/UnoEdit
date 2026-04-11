// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Text.RegularExpressions;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Generates VisualLineLinkText elements for URLs in the document.
	/// </summary>
	public class LinkElementGenerator : VisualLineElementGenerator
	{
		/// <summary>Creates a new LinkElementGenerator.</summary>
		public LinkElementGenerator() { }

		/// <summary>Gets/sets whether Ctrl must be held to activate links.</summary>
		public bool RequireControlModifierForClick { get; set; }

		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset) => -1;

		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset) => null;
	}
}
