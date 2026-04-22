using Microsoft.UI.Xaml;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Host interface implemented by platform TextView controls to allow shared rendering
	/// code to attach/detach inline UI elements produced by InlineObjectRun.
	/// </summary>
	public interface IInlineObjectHost
	{
		void AttachInlineElement(UIElement element);
		void DetachInlineElement(UIElement element);
	}
}
