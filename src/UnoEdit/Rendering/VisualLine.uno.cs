using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Represents a single logical "text line" in the Uno port.
	/// In Uno there is no word-wrap, so each VisualLine has exactly one UnoTextLine.
	/// </summary>
	public sealed class UnoTextLine
	{
		internal UnoTextLine(VisualLine owner) { Owner = owner; }

		/// <summary>Gets the VisualLine that owns this text line.</summary>
		public VisualLine Owner { get; }
	}

	public partial class VisualLine
	{
		internal void ConstructVisualElements(ITextRunConstructionContext context, VisualLineElementGenerator[] generators)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (generators == null)
				throw new ArgumentNullException(nameof(generators));

			Debug.Assert(phase == LifetimePhase.Generating);
			foreach (VisualLineElementGenerator generator in generators)
			{
				generator.StartGeneration(context);
			}

			elements = new List<VisualLineElement>();
			try
			{
				PerformVisualElementConstruction(generators);
			}
			finally
			{
				foreach (VisualLineElementGenerator generator in generators)
				{
					generator.FinishGeneration();
				}
			}

			foreach (VisualLineElement element in elements)
			{
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

			while (offset + askInterestOffset <= currentLineEnd)
			{
				int textPieceEndOffset = currentLineEnd;
				foreach (VisualLineElementGenerator generator in generators)
				{
					generator.cachedInterest = generator.GetFirstInterestedOffset(offset + askInterestOffset);
					if (generator.cachedInterest != -1)
					{
						if (generator.cachedInterest < offset)
						{
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
				if (textPieceEndOffset > offset)
				{
					int textPieceLength = textPieceEndOffset - offset;
					elements.Add(new VisualLineText(this, textPieceLength));
					offset = textPieceEndOffset;
				}

				askInterestOffset = 1;
				foreach (VisualLineElementGenerator generator in generators)
				{
					if (generator.cachedInterest == offset)
					{
						VisualLineElement element = generator.ConstructElement(offset);
						if (element != null)
						{
							elements.Add(element);
							if (element.DocumentLength > 0)
							{
								askInterestOffset = 0;
								offset += element.DocumentLength;
								if (offset > currentLineEnd)
								{
									DocumentLine newEndLine = document.GetLineByOffset(offset);
									currentLineEnd = newEndLine.Offset + newEndLine.Length;
									LastDocumentLine = newEndLine;
									if (currentLineEnd < offset)
									{
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
			foreach (IVisualLineTransformer transformer in transformers)
			{
				transformer.Transform(context, elements);
			}
			phase = LifetimePhase.Live;
		}

		partial void DisposeCore()
		{
		}

		// ---------------------------------------------------------------
		// Single-row TextLine abstraction for the Uno rendering model.
		// In Uno there is no word-wrap, so each VisualLine has exactly one
		// logical "text line".  We expose an UnoTextLine sentinel to satisfy
		// callers that depend on the WPF TextFormatter-based API.
		// ---------------------------------------------------------------

		private ReadOnlyCollection<UnoTextLine> _textLines;

		/// <summary>
		/// Gets the text lines for this visual line.
		/// In the Uno port each visual line has exactly one text line.
		/// </summary>
		public ReadOnlyCollection<UnoTextLine> TextLines
		{
			get
			{
				if (_textLines == null)
					_textLines = new ReadOnlyCollection<UnoTextLine>(
							new List<UnoTextLine> { new UnoTextLine(this) });
				return _textLines;
			}
		}

		/// <summary>Gets the text line at the specified visual Y position.</summary>
		public UnoTextLine GetTextLineByVisualYPosition(double visualTop)
		{
			// Single-line model: always return the one text line.
			return TextLines[0];
		}

		/// <summary>
		/// Gets the visual Y position of the specified text line.
		/// </summary>
		/// <param name="textLine">The UnoTextLine (must belong to this visual line).</param>
		/// <param name="yPositionMode">One of the <see cref="VisualYPosition"/> constants (passed as int/object).</param>
		public double GetTextLineVisualYPosition(UnoTextLine textLine, VisualYPosition yPositionMode)
		{
			// textLine must be our single line − validate by identity
			if (textLine == null || textLine.Owner != this)
				return VisualTop;

			switch (yPositionMode)
			{
				case VisualYPosition.LineTop: return VisualTop;
				case VisualYPosition.LineMiddle: return VisualTop + Height / 2;
				case VisualYPosition.LineBottom: return VisualTop + Height;
				case VisualYPosition.TextTop: return VisualTop;
				case VisualYPosition.TextMiddle: return VisualTop + Height / 2;
				case VisualYPosition.TextBottom: return VisualTop + Height;
				case VisualYPosition.Baseline: return VisualTop + Height * 0.8;
				default: return VisualTop;
			}
		}

		/// <summary>Gets the visual start column of the specified text line in the single-line Uno model.</summary>
		public int GetTextLineVisualStartColumn(UnoTextLine textLine)
		{
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));
			if (textLine.Owner != this)
				throw new ArgumentException("The text line does not belong to this visual line.", nameof(textLine));

			int startColumn = 0;
			return startColumn;
		}

		/// <summary>Gets the text line for the specified visual column.</summary>
		public UnoTextLine GetTextLine(int visualColumn, bool isAtEndOfLine = false)
		{
			// Single-line model: always the first line.
			return TextLines[0];
		}

		/// <summary>
		/// Gets the visual X position of the specified visual column in the text line.
		/// Returns 0 because Uno does not expose per-column X metrics from this layer.
		/// </summary>
		public double GetTextLineVisualXPosition(UnoTextLine textLine, int visualColumn) => 0.0;

		/// <summary>
		/// Gets the visual position of the specified visual column.
		/// X is approximated as 0 (column-level X not available in Uno rendering layer).
		/// Y is computed from <see cref="VisualTop"/> and the requested mode.
		/// </summary>
		public Windows.Foundation.Point GetVisualPosition(int visualColumn, VisualYPosition yPositionMode)
		{
			double y = GetTextLineVisualYPosition(TextLines[0], yPositionMode);
			return new Windows.Foundation.Point(0, y);
		}

		/// <summary>
		/// Gets the visual column at the specified visual position (floor mode).
		/// Returns 0 because column-level X metrics are not available in Uno rendering layer.
		/// </summary>
		public int GetVisualColumnFloor(Windows.Foundation.Point visualPosition, bool allowVirtualSpace = false) => 0;

		/// <summary>
		/// Gets the TextViewPosition at the visual position (floor mode).
		/// Line/column are resolved from document offset; X mapping is not available.
		/// </summary>
		public TextViewPosition GetTextViewPositionFloor(Windows.Foundation.Point visualPosition, bool allowVirtualSpace = false)
		{
			var dl = FirstDocumentLine;
			if (dl == null) return default;
			return new TextViewPosition(dl.LineNumber, 1);
		}
	}
}
