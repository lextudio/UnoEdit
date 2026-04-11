// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Exposes the AvalonEdit-compatible Caret public surface.
	/// Backing position state is updated by the owning TextArea via
	/// <see cref="Update"/> / <see cref="SetPosition"/>.
	/// </summary>
	public sealed partial class Caret
	{
		// Backing state — updated externally by the Uno TextArea.
		private int    _offset;
		private int    _line   = 1;
		private int    _column = 1;
		private int    _visualColumn;

		// Optional callback for BringCaretToView, injected by the owning TextArea.
		private readonly Action _bringIntoView;

		// Optional callback to set the caret offset on the owning TextArea.
		private readonly Action<int> _setOffset;

		/// <summary>
		/// Creates a standalone Caret with no owner.
		/// </summary>
		public Caret() { }

		/// <summary>
		/// Creates a Caret backed by callbacks from the owning TextArea.
		/// </summary>
		/// <param name="bringIntoView">Called by <see cref="BringCaretToView"/>.</param>
		/// <param name="setOffset">Called when the caller writes <see cref="Offset"/> or <see cref="Position"/>.</param>
		internal Caret(Action bringIntoView, Action<int> setOffset)
		{
			_bringIntoView = bringIntoView;
			_setOffset     = setOffset;
		}

		// === Internal state management called by the owning TextArea ===

		/// <summary>
		/// Updates the caret state from the owning TextArea.
		/// Raises <see cref="PositionChanged"/> if the position actually changed.
		/// </summary>
		internal void Update(int offset, int line, int column, int visualColumn = 0)
		{
			bool changed = _offset != offset || _line != line || _column != column || _visualColumn != visualColumn;
			_offset      = offset;
			_line        = line;
			_column      = column;
			_visualColumn = visualColumn;
			if (changed)
				PositionChanged?.Invoke(this, EventArgs.Empty);
		}

		// === AvalonEdit-compatible public surface ===

		/// <summary>Gets/sets the caret position as a TextViewPosition (line/column/visual column).</summary>
		public TextViewPosition Position
		{
			get => new TextViewPosition(_line, _column, _visualColumn);
			set
			{
				if (_setOffset != null && value.Line >= 1 && value.Column >= 1)
				{
					// Convert line/col back to offset via the callback — the TextArea will call Update().
					_setOffset(-(value.Line * 100000 + value.Column)); // sentinel for line/col set; TextArea interprets
				}
			}
		}

		/// <summary>Gets the caret's document location (line/column).</summary>
		public TextLocation Location => new TextLocation(_line, _column);

		/// <summary>Gets/sets the caret line number (1-based).</summary>
		public int Line
		{
			get => _line;
			set => Position = new TextViewPosition(value, _column, _visualColumn);
		}

		/// <summary>Gets/sets the caret column number (1-based).</summary>
		public int Column
		{
			get => _column;
			set => Position = new TextViewPosition(_line, value, _visualColumn);
		}

		/// <summary>Gets/sets the caret visual column.</summary>
		public int VisualColumn
		{
			get => _visualColumn;
			set => Position = new TextViewPosition(_line, _column, value);
		}

		/// <summary>Gets whether the caret is in virtual space (past line end).</summary>
		public bool IsInVirtualSpace => _visualColumn > _column - 1;

		/// <summary>Gets/sets the caret document offset.</summary>
		public int Offset
		{
			get => _offset;
			set => _setOffset?.Invoke(value);
		}

		/// <summary>
		/// Gets/sets the desired horizontal screen position.
		/// Used to maintain column position when moving up/down.
		/// </summary>
		public double DesiredXPos { get; set; } = double.NaN;

		/// <summary>Raised when the caret position changes.</summary>
		public event EventHandler PositionChanged;

		/// <summary>Returns the screen rectangle of the caret. Returns Rect.Empty when unavailable.</summary>
		public Rect CalculateCaretRectangle() => Rect.Empty;

		/// <summary>Scrolls the view so the caret is visible.</summary>
		public void BringCaretToView() => _bringIntoView?.Invoke();

		/// <summary>Makes the caret visible (visibility is managed by the renderer).</summary>
		public void Show() { }

		/// <summary>Hides the caret (visibility is managed by the renderer).</summary>
		public void Hide() { }

		// === Uno-specific rendering property ===

		Brush _caretBrush;

		/// <summary>Gets/Sets the brush used to render the caret.</summary>
		public Brush CaretBrush
		{
			get => _caretBrush;
			set => _caretBrush = value;
		}
	}
}
