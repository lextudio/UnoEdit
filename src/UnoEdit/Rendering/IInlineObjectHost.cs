using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace ICSharpCode.AvalonEdit.Rendering
{
	public readonly struct InlineElementMetrics
	{
		public InlineElementMetrics(Size size, double baseline)
		{
			Size = size;
			Baseline = baseline;
		}

		public Size Size { get; }
		public double Baseline { get; }
	}

	/// <summary>
	/// Host interface implemented by platform TextView controls to allow shared rendering
	/// code to attach/detach inline UI elements produced by InlineObjectRun.
	/// </summary>
	public interface IInlineObjectHost
	{
		void AttachInlineElement(UIElement element);
		void DetachInlineElement(UIElement element);
		InlineElementMetrics MeasureInlineElement(UIElement element);
	}
}
