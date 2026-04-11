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
			: base(parentVisualLine, length)
		{
			RequireControlModifierForClick = true;
		}

		/// <inheritdoc/>
		public override object CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			if (context != null) {
				TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
				TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
				if (context.TextView.LinkTextUnderline)
					TextRunProperties.SetTextDecorations("Underline");
			}

			var baseRun = base.CreateTextRun(startVisualColumn, context) as TextRunDescriptor;
			if (baseRun == null)
				return new TextRunDescriptor("link", string.Empty, startVisualColumn, 0, TextRunProperties,
					new LinkRunMetadata(NavigateUri, TargetName, RequireControlModifierForClick));

			return new TextRunDescriptor("link", baseRun.Text, baseRun.StartVisualColumn, baseRun.Length, baseRun.Properties,
				new LinkRunMetadata(NavigateUri, TargetName, RequireControlModifierForClick));
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
	}
}
