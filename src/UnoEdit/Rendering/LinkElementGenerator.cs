// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Generates VisualLineLinkText elements for URLs in the document.
	/// </summary>
	public class LinkElementGenerator : VisualLineElementGenerator
	{
		// A link starts with a protocol (or just www), followed by link characters, followed by a link end character.
		internal static readonly Regex defaultLinkRegex = new Regex(@"\b(https?://|ftp://|www\.)[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]");
		internal static readonly Regex defaultMailRegex = new Regex(@"\b[\w\d\.\-]+\@[\w\d\.\-]+\.[a-z]{2,6}\b");

		private readonly Regex linkRegex;

		/// <summary>Creates a new LinkElementGenerator.</summary>
		public LinkElementGenerator()
		{
			this.linkRegex = defaultLinkRegex;
			this.RequireControlModifierForClick = true;
		}

		/// <summary>Creates a new LinkElementGenerator using the specified regex.</summary>
		protected LinkElementGenerator(Regex regex) : this()
		{
			this.linkRegex = regex ?? throw new ArgumentNullException(nameof(regex));
		}

		/// <summary>Gets/sets whether Ctrl must be held to activate links.</summary>
		public bool RequireControlModifierForClick { get; set; }

		private Match GetMatch(int startOffset, out int matchOffset)
		{
			int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
			StringSegment relevantText = CurrentContext.GetText(startOffset, endOffset - startOffset);
			Match m = linkRegex.Match(relevantText.Text, relevantText.Offset, relevantText.Count);
			matchOffset = m.Success ? m.Index - relevantText.Offset + startOffset : -1;
			return m;
		}

		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset)
		{
			GetMatch(startOffset, out int matchOffset);
			return matchOffset;
		}

		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset)
		{
			Match m = GetMatch(offset, out int matchOffset);
			if (m.Success && matchOffset == offset)
				return ConstructElementFromMatch(m);
			return null;
		}

		/// <summary>Constructs a VisualLineElement from a regex match.</summary>
		protected virtual VisualLineElement ConstructElementFromMatch(Match m)
		{
			Uri uri = GetUriFromMatch(m);
			if (uri == null)
				return null;
			var linkText = new VisualLineLinkText(CurrentContext.VisualLine, m.Length);
			linkText.NavigateUri = uri;
			linkText.RequireControlModifierForClick = this.RequireControlModifierForClick;
			return linkText;
		}

		/// <summary>Fetches the URI from the regex match.</summary>
		protected virtual Uri GetUriFromMatch(Match match)
		{
			string targetUrl = match.Value;
			if (targetUrl.StartsWith("www.", StringComparison.Ordinal))
				targetUrl = "http://" + targetUrl;
			if (Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
				return new Uri(targetUrl);
			return null;
		}
	}
}
