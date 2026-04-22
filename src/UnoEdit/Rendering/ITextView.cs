using System;
using Microsoft.UI.Dispatching;

namespace ICSharpCode.AvalonEdit.Rendering
{
	public interface ITextView
	{
		event EventHandler VisibleLinesChanged;
		event EventHandler ScrollOffsetChanged;
		int FirstVisibleLineNumber { get; }
		int LastVisibleLineNumber { get; }
		DispatcherQueue DispatcherQueue { get; }
	}
}
