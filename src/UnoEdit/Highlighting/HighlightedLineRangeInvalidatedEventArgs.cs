using System;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Describes an inclusive 1-based document line range whose highlighting changed.
	/// </summary>
	public sealed class HighlightedLineRangeInvalidatedEventArgs : EventArgs
	{
		public HighlightedLineRangeInvalidatedEventArgs(int startLineNumber, int endLineNumber)
		{
			StartLineNumber = startLineNumber;
			EndLineNumber = endLineNumber;
		}

		public int StartLineNumber { get; }
		public int EndLineNumber { get; }
	}
}
