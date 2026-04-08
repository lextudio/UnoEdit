// Forked from AvalonEdit for UnoEdit — WPF Brush/ITextRunConstructionContext removed.
// Original: ICSharpCode.AvalonEdit/Highlighting/HighlightingBrush.cs

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A brush used for syntax highlighting.  Returns a portable Color on demand.
	/// </summary>
	[Serializable]
	public abstract class HighlightingBrush
	{
		/// <summary>Gets the color represented by this brush.  May return null.</summary>
		public abstract Color? GetColor();
	}

	/// <summary>
	/// Highlighting brush backed by a fixed color value.
	/// </summary>
	[Serializable]
	public sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
	{
		readonly Color _color;

		/// <summary>Creates a new brush for the given color.</summary>
		public SimpleHighlightingBrush(Color color) { _color = color; }

		/// <inheritdoc/>
		public override Color? GetColor() => _color;

		/// <inheritdoc/>
		public override string ToString() => _color.ToString();

		SimpleHighlightingBrush(SerializationInfo info, StreamingContext context)
		{
			_color = (Color)new ColorConverter().ConvertFromInvariantString(info.GetString("color"))!;
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("color", _color.ToString());
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return obj is SimpleHighlightingBrush other && _color == other._color;
		}

		/// <inheritdoc/>
		public override int GetHashCode() => _color.GetHashCode();
	}
}
