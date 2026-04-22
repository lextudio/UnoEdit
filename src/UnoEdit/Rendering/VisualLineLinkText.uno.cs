using System;
using System.Diagnostics;

namespace ICSharpCode.AvalonEdit.Rendering
{
	public partial class VisualLineLinkText
	{
		internal bool TryOpen()
		{
			if (!LinkIsClickable())
				return false;

			try {
				Process.Start(new ProcessStartInfo { FileName = NavigateUri.ToString(), UseShellExecute = true });
				return true;
			} catch {
				return false;
			}
		}
	}
}
