// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Windows.Media.TextFormatting;

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
			: base(parentVisualLine, length)
		{
			RequireControlModifierForClick = true;
		}

		/// <inheritdoc/>
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			if (context != null) {
				TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
				TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
				if (context.TextView.LinkTextUnderline)
					TextRunProperties.SetTextDecorations(System.Windows.Media.TextDecorations.Underline);
			}

			return base.CreateTextRun(startVisualColumn, context);
		}

		public sealed class LinkRunMetadata
		{
			public LinkRunMetadata(Uri navigateUri, string targetName, bool requireControlModifierForClick)
			{
				NavigateUri = navigateUri;
				TargetName = targetName;
				RequireControlModifierForClick = requireControlModifierForClick;
			}

			public Uri NavigateUri { get; }
			public string TargetName { get; }
			public bool RequireControlModifierForClick { get; }
		}

		protected override VisualLineText CreateInstance(int length)
		{
			return new VisualLineLinkText(ParentVisualLine, length) {
				NavigateUri = NavigateUri,
				TargetName = TargetName,
				RequireControlModifierForClick = RequireControlModifierForClick
			};
		}
	}
}
