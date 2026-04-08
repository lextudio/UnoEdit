// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team (original AvalonEdit)
// Ported to UnoEdit — WPF rendering dependencies removed.

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A section that can be folded.
	/// </summary>
	public sealed class FoldingSection : TextSegment
	{
		readonly FoldingManager manager;
		bool isFolded;
		string title;

		/// <summary>
		/// Gets/sets whether this section is currently folded (collapsed).
		/// </summary>
		public bool IsFolded {
			get { return isFolded; }
			set {
				if (isFolded != value) {
					isFolded = value;
					manager.RaiseFoldingsChanged();
				}
			}
		}

		/// <summary>
		/// Gets/sets the text shown in place of the collapsed content.
		/// </summary>
		public string Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					if (isFolded)
						manager.RaiseFoldingsChanged();
				}
			}
		}

		/// <summary>
		/// Returns the raw text covered by this folding.
		/// </summary>
		public string TextContent {
			get { return manager.Document.GetText(StartOffset, EndOffset - StartOffset); }
		}

		/// <summary>Gets/sets an arbitrary tag associated with this section.</summary>
		public object Tag { get; set; }

		internal FoldingSection(FoldingManager manager, int startOffset, int endOffset)
		{
			this.manager = manager;
			this.StartOffset = startOffset;
			this.Length = endOffset - startOffset;
		}

		/// <inheritdoc/>
		protected override void OnSegmentChanged()
		{
			base.OnSegmentChanged();
			if (IsConnectedToCollection)
				manager.RaiseFoldingsChanged();
		}
	}
}
