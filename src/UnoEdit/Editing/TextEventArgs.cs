using System;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Event arguments carrying copied text.
	/// </summary>
	[Serializable]
	public class TextEventArgs : EventArgs
	{
		readonly string text;

		public string Text => text;

		public TextEventArgs(string text)
		{
			this.text = text ?? throw new ArgumentNullException(nameof(text));
		}
	}
}
