// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Inline link element in the visual line.
	/// </summary>
	public class VisualLineLinkText : VisualLineText
	{
		/// <summary>Gets/sets the URI to navigate to.</summary>
		public Uri NavigateUri { get; set; }

		/// <summary>Gets/sets the target frame name.</summary>
		public string TargetName { get; set; }

		/// <summary>Gets/sets whether Ctrl must be held to activate the link.</summary>
		public bool RequireControlModifierForClick { get; set; }

		/// <summary>Creates a new VisualLineLinkText.</summary>
		public VisualLineLinkText(VisualLine parentVisualLine, int length)
			: base(parentVisualLine, length) { }

		/// <inheritdoc/>
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context) => null;
	}
}
