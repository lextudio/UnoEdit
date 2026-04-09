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
	}
}
