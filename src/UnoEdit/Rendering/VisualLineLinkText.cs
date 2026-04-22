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
using System.Windows.Media.TextFormatting;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// VisualLineElement that represents a piece of text and is a clickable link.
	/// </summary>
	public partial class VisualLineLinkText : VisualLineText
	{
		/// <summary>
		/// Gets/Sets the URL that is navigated to when the link is clicked.
		/// </summary>
		public Uri NavigateUri { get; set; }

		/// <summary>
		/// Gets/Sets the window name where the URL will be opened.
		/// </summary>
		public string TargetName { get; set; }

		/// <summary>
		/// Gets/Sets whether the user needs to press Control to click the link.
		/// The default value is true.
		/// </summary>
		public bool RequireControlModifierForClick { get; set; }

		/// <summary>
		/// Creates a visual line text element with the specified length.
		/// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
		/// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
		/// </summary>
		public VisualLineLinkText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
		{
			this.RequireControlModifierForClick = true;
		}

		/// <inheritdoc/>
		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			this.TextRunProperties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
			this.TextRunProperties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
			if (context.TextView.LinkTextUnderline)
				this.TextRunProperties.SetTextDecorations(System.Windows.Media.TextDecorations.Underline);
			return base.CreateTextRun(startVisualColumn, context);
		}

		/// <summary>
		/// Gets whether the link is currently clickable.
		/// </summary>
		/// <remarks>Platform partials can refine this when modifier-state information is available.</remarks>
		protected virtual bool LinkIsClickable()
		{
			return NavigateUri != null;
		}

		protected override VisualLineText CreateInstance(int length)
		{
			return new VisualLineLinkText(ParentVisualLine, length) {
				NavigateUri = this.NavigateUri,
				TargetName = this.TargetName,
				RequireControlModifierForClick = this.RequireControlModifierForClick
			};
		}
	}
}
