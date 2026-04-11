using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Contains classes for handling weak events on the Caret class.
	/// </summary>
	public static class CaretWeakEventManager
	{
		/// <summary>
		/// Handles the Caret.PositionChanged event.
		/// </summary>
		public sealed class PositionChanged : WeakEventManagerBase<PositionChanged, Caret>
		{
			/// <inheritdoc/>
			protected override void StartListening(Caret source)
			{
				source.PositionChanged += DeliverEvent;
			}

			/// <inheritdoc/>
			protected override void StopListening(Caret source)
			{
				source.PositionChanged -= DeliverEvent;
			}
		}
	}
}
