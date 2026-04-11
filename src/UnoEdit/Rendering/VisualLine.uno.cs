using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	public partial class VisualLine
	{
		internal void ConstructVisualElements(ITextRunConstructionContext context, VisualLineElementGenerator[] generators)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (generators == null)
				throw new ArgumentNullException(nameof(generators));

			Debug.Assert(phase == LifetimePhase.Generating);
			foreach (VisualLineElementGenerator generator in generators) {
				generator.StartGeneration(context);
			}

			elements = new List<VisualLineElement>();
			try {
				PerformVisualElementConstruction(generators);
			} finally {
				foreach (VisualLineElementGenerator generator in generators) {
					generator.FinishGeneration();
				}
			}

			foreach (VisualLineElement element in elements) {
				element.SetTextRunProperties(new VisualLineElementTextRunProperties());
			}

			Elements = new ReadOnlyCollection<VisualLineElement>(elements);
			CalculateOffsets();
			phase = LifetimePhase.Transforming;
		}

		void PerformVisualElementConstruction(VisualLineElementGenerator[] generators)
		{
			TextDocument document = Document;
			int offset = FirstDocumentLine.Offset;
			int currentLineEnd = offset + FirstDocumentLine.Length;
			LastDocumentLine = FirstDocumentLine;
			int askInterestOffset = 0;

			while (offset + askInterestOffset <= currentLineEnd) {
				int textPieceEndOffset = currentLineEnd;
				foreach (VisualLineElementGenerator generator in generators) {
					generator.cachedInterest = generator.GetFirstInterestedOffset(offset + askInterestOffset);
					if (generator.cachedInterest != -1) {
						if (generator.cachedInterest < offset) {
							throw new ArgumentOutOfRangeException(
								generator.GetType().Name + ".GetFirstInterestedOffset",
								generator.cachedInterest,
								"GetFirstInterestedOffset must not return an offset less than startOffset. Return -1 to signal no interest.");
						}
						if (generator.cachedInterest < textPieceEndOffset)
							textPieceEndOffset = generator.cachedInterest;
					}
				}

				Debug.Assert(textPieceEndOffset >= offset);
				if (textPieceEndOffset > offset) {
					int textPieceLength = textPieceEndOffset - offset;
					elements.Add(new VisualLineText(this, textPieceLength));
					offset = textPieceEndOffset;
				}

				askInterestOffset = 1;
				foreach (VisualLineElementGenerator generator in generators) {
					if (generator.cachedInterest == offset) {
						VisualLineElement element = generator.ConstructElement(offset);
						if (element != null) {
							elements.Add(element);
							if (element.DocumentLength > 0) {
								askInterestOffset = 0;
								offset += element.DocumentLength;
								if (offset > currentLineEnd) {
									DocumentLine newEndLine = document.GetLineByOffset(offset);
									currentLineEnd = newEndLine.Offset + newEndLine.Length;
									LastDocumentLine = newEndLine;
									if (currentLineEnd < offset) {
										throw new InvalidOperationException(
											"The VisualLineElementGenerator " + generator.GetType().Name +
											" produced an element which ends within the line delimiter");
									}
								}
								break;
							}
						}
					}
				}
			}
		}

		internal void RunTransformers(ITextRunConstructionContext context, IVisualLineTransformer[] transformers)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (transformers == null)
				throw new ArgumentNullException(nameof(transformers));

			Debug.Assert(phase == LifetimePhase.Transforming);
			foreach (IVisualLineTransformer transformer in transformers) {
				transformer.Transform(context, elements);
			}
			phase = LifetimePhase.Live;
		}

		partial void DisposeCore()
		{
		}

		// Stubs for WPF TextLine-based members missing from VisualLine.cs
		// These relate to WPF TextFormatter text lines; we expose stub versions.

		/// <summary>Gets the text lines for this visual line (stub).</summary>
		public System.Collections.ObjectModel.ReadOnlyCollection<object> TextLines { get; } =
			new System.Collections.ObjectModel.ReadOnlyCollection<object>(new System.Collections.Generic.List<object>());

		/// <summary>Gets the text line at the specified visual Y position (stub).</summary>
		public object GetTextLineByVisualYPosition(double visualTop) => null;

		/// <summary>Gets the visual Y position of the specified text line (stub).</summary>
		public double GetTextLineVisualYPosition(object textLine, object yPositionMode) => 0.0;

		/// <summary>Gets the visual start column of the specified text line (stub).</summary>
		public int GetTextLineVisualStartColumn(object textLine) => 0;

		/// <summary>Gets the text line for the specified visual column (stub).</summary>
		public object GetTextLine(int visualColumn, bool isAtEndOfLine = false) => null;

		/// <summary>Gets the visual X position of the specified column in the text line (stub).</summary>
		public double GetTextLineVisualXPosition(object textLine, int visualColumn) => 0.0;

		/// <summary>Gets the visual position of the specified visual column (stub).</summary>
		public Windows.Foundation.Point GetVisualPosition(int visualColumn, object yPositionMode) => default;

		/// <summary>Gets the visual column at the specified X position, flooring to previous (stub).</summary>
		public int GetVisualColumnFloor(Windows.Foundation.Point visualPosition, bool allowVirtualSpace = false) => 0;

		/// <summary>Gets the TextViewPosition at the visual column, flooring to previous (stub).</summary>
		public TextViewPosition GetTextViewPositionFloor(Windows.Foundation.Point visualPosition, bool allowVirtualSpace = false) => default;
	}
}
