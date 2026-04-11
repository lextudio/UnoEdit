// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Highlighting;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Builds highlighted inline runs from rich text.
	/// Not used in AvalonEdit rendering itself but useful for external consumers.
	/// </summary>
	public sealed class HighlightedInlineBuilder
	{
		readonly string text;
		List<int> stateChangeOffsets = new List<int>();
		List<HighlightingColor> stateChanges = new List<HighlightingColor>();

		/// <summary>Creates a new HighlightedInlineBuilder for the given text.</summary>
		public HighlightedInlineBuilder(string text)
		{
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			stateChangeOffsets.Add(0);
			stateChanges.Add(new HighlightingColor());
		}

		/// <summary>Creates a new HighlightedInlineBuilder from a RichText.</summary>
		public HighlightedInlineBuilder(RichText text)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			this.text = text.Text;
			stateChangeOffsets.AddRange(text.stateChangeOffsets);
			stateChanges.AddRange(text.stateChanges.Select(s => s.Clone()));
		}

		HighlightedInlineBuilder(string text, List<int> offsets, List<HighlightingColor> states)
		{
			this.text = text;
			stateChangeOffsets = offsets;
			stateChanges = states;
		}

		/// <summary>Gets the text.</summary>
		public string Text => text;

		/// <summary>Applies a highlighting color to a range.</summary>
		public void SetHighlighting(int offset, int length, HighlightingColor color)
		{
			if (color == null)
				throw new ArgumentNullException(nameof(color));
			if (color.Foreground == null && color.Background == null && color.FontStyle == null && color.FontWeight == null && color.Underline == null)
				return;

			var startIndex = GetIndexForOffset(offset);
			var endIndex = GetIndexForOffset(offset + length);
			for (var i = startIndex; i < endIndex; i++)
				stateChanges[i].MergeWith(color);
		}

		/// <summary>Sets the foreground brush for a range.</summary>
		public void SetForeground(int offset, int length, object brush)
		{
			var startIndex = GetIndexForOffset(offset);
			var endIndex = GetIndexForOffset(offset + length);
			var hBrush = ToHighlightingBrush(brush);
			for (var i = startIndex; i < endIndex; i++)
				stateChanges[i].Foreground = hBrush;
		}

		/// <summary>Sets the background brush for a range.</summary>
		public void SetBackground(int offset, int length, object brush)
		{
			var startIndex = GetIndexForOffset(offset);
			var endIndex = GetIndexForOffset(offset + length);
			var hBrush = ToHighlightingBrush(brush);
			for (var i = startIndex; i < endIndex; i++)
				stateChanges[i].Background = hBrush;
		}

		/// <summary>Sets the font weight for a range.</summary>
		public void SetFontWeight(int offset, int length, object weight)
		{
			if (!(weight is System.Windows.FontWeight w))
				return;
			var startIndex = GetIndexForOffset(offset);
			var endIndex = GetIndexForOffset(offset + length);
			for (var i = startIndex; i < endIndex; i++)
				stateChanges[i].FontWeight = w;
		}

		/// <summary>Sets the font style for a range.</summary>
		public void SetFontStyle(int offset, int length, object style)
		{
			if (!(style is System.Windows.FontStyle s))
				return;
			var startIndex = GetIndexForOffset(offset);
			var endIndex = GetIndexForOffset(offset + length);
			for (var i = startIndex; i < endIndex; i++)
				stateChanges[i].FontStyle = s;
		}

		/// <summary>Creates inline run objects.</summary>
		public object[] CreateRuns()
		{
			if (text.Length == 0)
				return Array.Empty<object>();

			var runs = new List<object>();
			for (var i = 0; i < stateChangeOffsets.Count; i++) {
				var start = stateChangeOffsets[i];
				var end = i + 1 < stateChangeOffsets.Count ? stateChangeOffsets[i + 1] : text.Length;
				if (end <= start)
					continue;

				var run = new Run(text.Substring(start, end - start));
				RichText.ApplyColorToTextElement(run, stateChanges[i]);
				runs.Add(run);
			}
			return runs.ToArray();
		}

		/// <summary>Converts to a RichText.</summary>
		public RichText ToRichText()
		{
			return new RichText(text, stateChangeOffsets.ToArray(), stateChanges.Select(s => s.Clone()).ToArray());
		}

		/// <summary>Creates a clone of this builder.</summary>
		public HighlightedInlineBuilder Clone()
		{
			return new HighlightedInlineBuilder(text, stateChangeOffsets.ToList(), stateChanges.Select(s => s.Clone()).ToList());
		}

		int GetIndexForOffset(int offset)
		{
			if (offset < 0 || offset > text.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));

			var index = stateChangeOffsets.BinarySearch(offset);
			if (index < 0) {
				index = ~index;
				if (offset < text.Length) {
					stateChanges.Insert(index, stateChanges[index - 1].Clone());
					stateChangeOffsets.Insert(index, offset);
				}
			}
			return index;
		}

		static HighlightingBrush ToHighlightingBrush(object brush)
		{
			if (brush is HighlightingBrush hb)
				return hb;

			var brushType = brush?.GetType();
			if (brushType != null && string.Equals(brushType.Name, "SolidColorBrush", StringComparison.Ordinal)) {
				var colorProperty = brushType.GetProperty("Color", BindingFlags.Public | BindingFlags.Instance);
				var colorValue = colorProperty?.GetValue(brush);
				if (TryConvertColor(colorValue, out var color))
					return new SimpleHighlightingBrush(color);
			}

			return null;
		}

		static bool TryConvertColor(object colorValue, out System.Windows.Media.Color color)
		{
			color = default;
			if (colorValue == null)
				return false;

			var type = colorValue.GetType();
			var a = type.GetProperty("A", BindingFlags.Public | BindingFlags.Instance)?.GetValue(colorValue);
			var r = type.GetProperty("R", BindingFlags.Public | BindingFlags.Instance)?.GetValue(colorValue);
			var g = type.GetProperty("G", BindingFlags.Public | BindingFlags.Instance)?.GetValue(colorValue);
			var b = type.GetProperty("B", BindingFlags.Public | BindingFlags.Instance)?.GetValue(colorValue);
			if (a is byte ba && r is byte br && g is byte bg && b is byte bb) {
				color = System.Windows.Media.Color.FromArgb(ba, br, bg, bb);
				return true;
			}

			return false;
		}
	}
}
