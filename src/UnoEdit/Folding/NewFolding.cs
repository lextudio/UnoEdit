// Copyright (c) 2009 Daniel Grunwald (original AvalonEdit)
// Ported to UnoEdit — no WPF dependencies.

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A proposed new folding produced by a folding strategy.
	/// </summary>
	public class NewFolding
	{
		/// <summary>Start offset of the folding.</summary>
		public int StartOffset { get; set; }

		/// <summary>End offset of the folding.</summary>
		public int EndOffset { get; set; }

		/// <summary>Display name of the folding (shown when collapsed).</summary>
		public string Name { get; set; } = "...";

		/// <summary>If true the folding is collapsed when first created.</summary>
		public bool DefaultClosed { get; set; }

		/// <summary>Creates a NewFolding.</summary>
		public NewFolding() { }

		/// <summary>Creates a NewFolding with the given start and end offsets.</summary>
		public NewFolding(int startOffset, int endOffset)
		{
			StartOffset = startOffset;
			EndOffset = endOffset;
		}
	}
}
