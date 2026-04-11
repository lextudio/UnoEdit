// Forked from AvalonEdit for UnoEdit — WPF Run/TextElement helpers removed.
// Original: ICSharpCode.AvalonEdit/Highlighting/RichText.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using System.Windows.Documents;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Represents immutable text with highlighting information.
	/// </summary>
	public class RichText
	{
		public static readonly RichText Empty = new RichText(string.Empty);

		readonly string text;
		internal readonly int[] stateChangeOffsets;
		internal readonly HighlightingColor[] stateChanges;

		public RichText(string text, RichTextModel model = null)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			this.text = text;
			if (model != null) {
				var sections = model.GetHighlightedSections(0, text.Length).ToArray();
				stateChangeOffsets = new int[sections.Length];
				stateChanges = new HighlightingColor[sections.Length];
				for (int i = 0; i < sections.Length; i++) {
					stateChangeOffsets[i] = sections[i].Offset;
					stateChanges[i] = sections[i].Color;
				}
			} else {
				stateChangeOffsets = new int[] { 0 };
				stateChanges = new HighlightingColor[] { HighlightingColor.Empty };
			}
		}

		internal RichText(string text, int[] offsets, HighlightingColor[] states)
		{
			this.text = text;
			Debug.Assert(offsets[0] == 0);
			Debug.Assert(offsets.Last() <= text.Length);
			stateChangeOffsets = offsets;
			stateChanges = states;
		}

		public string Text => text;
		public int Length => text.Length;

		int GetIndexForOffset(int offset)
		{
			if (offset < 0 || offset > text.Length)
				throw new ArgumentOutOfRangeException("offset");
			int index = Array.BinarySearch(stateChangeOffsets, offset);
			if (index < 0)
				index = ~index - 1;
			return index;
		}

		int GetEnd(int index)
		{
			if (index + 1 < stateChangeOffsets.Length)
				return stateChangeOffsets[index + 1];
			return text.Length;
		}

		public HighlightingColor GetHighlightingAt(int offset)
		{
			return stateChanges[GetIndexForOffset(offset)];
		}

		public IEnumerable<HighlightedSection> GetHighlightedSections(int offset, int length)
		{
			int index = GetIndexForOffset(offset);
			int pos = offset;
			int endOffset = offset + length;
			while (pos < endOffset) {
				int endPos = Math.Min(endOffset, GetEnd(index));
				yield return new HighlightedSection {
					Offset = pos,
					Length = endPos - pos,
					Color = stateChanges[index]
				};
				pos = endPos;
				index++;
			}
		}

		public RichTextModel ToRichTextModel()
		{
			return new RichTextModel(stateChangeOffsets, stateChanges.Select(ch => ch.Clone()).ToArray());
		}

		internal static void ApplyColorToTextElement(TextElement r, HighlightingColor state)
		{
			if (state.FontWeight != null)
				r.FontWeight = state.FontWeight.Value;
			if (state.FontStyle != null)
				r.FontStyle = state.FontStyle.Value;
			// Foreground/background are intentionally not mapped here because the
			// shared Uno highlighting stack no longer exposes a WPF Brush API.
		}

		public string ToHtml(HtmlOptions options = null)
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
				htmlWriter.Write(this);
			}
			return stringWriter.ToString();
		}

		public string ToHtml(int offset, int length, HtmlOptions options = null)
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
				htmlWriter.Write(this, offset, length);
			}
			return stringWriter.ToString();
		}

		public RichText Substring(int offset, int length)
		{
			if (offset == 0 && length == Length)
				return this;
			string newText = text.Substring(offset, length);
			RichTextModel model = ToRichTextModel();
			OffsetChangeMap map = new OffsetChangeMap(2);
			map.Add(new OffsetChangeMapEntry(offset + length, text.Length - offset - length, 0));
			map.Add(new OffsetChangeMapEntry(0, offset, 0));
			model.UpdateOffsets(map);
			return new RichText(newText, model);
		}

		public static RichText Concat(params RichText[] texts)
		{
			if (texts == null || texts.Length == 0)
				return Empty;
			if (texts.Length == 1)
				return texts[0];
			string newText = string.Concat(texts.Select(txt => txt.text));
			RichTextModel model = texts[0].ToRichTextModel();
			int offset = texts[0].Length;
			for (int i = 1; i < texts.Length; i++) {
				model.Append(offset, texts[i].stateChangeOffsets, texts[i].stateChanges);
				offset += texts[i].Length;
			}
			return new RichText(newText, model);
		}

		public static RichText operator +(RichText a, RichText b)
		{
			return Concat(a, b);
		}

		public override string ToString()
		{
			return text;
		}

				/// <summary>Creates inline run objects from this rich text.</summary>
				public object[] CreateRuns() => System.Array.Empty<object>();
			}
}
