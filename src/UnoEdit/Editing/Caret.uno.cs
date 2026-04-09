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

using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Editing
{
	public sealed partial class Caret
	{
		// Uno caret brush (replaces WPF CaretLayer.CaretBrush)
		Brush _caretBrush;

		/// <summary>
		/// Gets/Sets the color of the caret.
		/// </summary>
		public Brush CaretBrush {
			get { return _caretBrush; }
			set { _caretBrush = value; }
		}

		partial void Initialize()
		{
			// TODO: set up Uno caret rendering layer when available
		}

		partial void InvalidateCaretVisual()
		{
			// TODO: invalidate Uno caret visual
		}

		partial void GetCaretWidthCore(ref double width)
		{
			// Use the system default of 1 device-independent pixel
			width = 1.0;
		}

		// ShowCaretAsync has no implementation: Show()'s synchronous fallback runs instead.

		partial void ShowCaretInternal(Rect caretRect)
		{
			// TODO: update Uno caret position/visibility
		}

		partial void HideCaretInternal()
		{
			// TODO: hide Uno caret
		}

		partial void DestroyWin32Caret()
		{
			// No Win32 caret on Uno
		}

		private partial Rect CalcCaretRectangle(VisualLine visualLine)
		{
			// TODO: implement using Uno text layout API
			return Rect.Empty;
		}

		private partial Rect CalcCaretOverstrikeRectangle(VisualLine visualLine)
		{
			// TODO: implement using Uno text layout API
			return Rect.Empty;
		}
	}
}
