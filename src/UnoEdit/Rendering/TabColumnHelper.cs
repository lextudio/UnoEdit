// Copyright (c) 2024 AlephNote Authors (see AUTHORS file)
// This code is distributed under the MIT license (see accompanying license.txt OR https://opensource.org/licenses/MIT)

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Utility for converting between logical (character-index) and visual (display) columns,
	/// accounting for tab characters expanding to the next tab stop.
	/// Columns are 0-based.
	/// </summary>
	public static class TabColumnHelper
	{
		/// <summary>Tab width in spaces.</summary>
		public const int TabWidth = 4;

		/// <summary>
		/// Compute the visual (display) column for a given logical column in a line's text,
		/// expanding tab characters to the next tab stop (multiple of <see cref="TabWidth"/>).
		/// </summary>
		public static int LogicalToVisualColumn(string text, int logicalColumn)
		{
			if (text == null) throw new System.ArgumentNullException(nameof(text));
			int visual = 0;
			for (int i = 0; i < logicalColumn && i < text.Length; i++)
			{
				if (text[i] == '\t')
					visual = ((visual / TabWidth) + 1) * TabWidth;
				else
					visual++;
			}
			return visual;
		}

		/// <summary>
		/// Compute the logical column closest to a given visual column.
		/// Returns the 0-based logical column index.
		/// </summary>
		public static int VisualToLogicalColumn(string text, int visualColumn)
		{
			if (text == null) throw new System.ArgumentNullException(nameof(text));
			int visual = 0;
			for (int i = 0; i < text.Length; i++)
			{
				int nextVisual = text[i] == '\t'
					? ((visual / TabWidth) + 1) * TabWidth
					: visual + 1;
				if (visualColumn <= (visual + nextVisual) / 2)
					return i;
				visual = nextVisual;
			}
			return text.Length;
		}
	}
}
