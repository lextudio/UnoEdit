// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — WPF rendering dependencies removed.

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>Tracks the context of a snippet insertion.</summary>
	public class InsertionContext
	{
		/// <summary>Creates a new InsertionContext.</summary>
		public InsertionContext(object textArea, int insertionPosition)
		{
			InsertionPosition = insertionPosition;
		}

		/// <summary>Gets the text area.</summary>
		public object TextArea { get; private set; }

		/// <summary>Gets the document.</summary>
		public TextDocument Document => null;

		/// <summary>Gets the selected text at insertion time.</summary>
		public string SelectedText { get; private set; } = string.Empty;

		/// <summary>Gets the indentation string.</summary>
		public string Indentation { get; private set; } = string.Empty;

		/// <summary>Gets the tab string.</summary>
		public string Tab { get; private set; } = "\t";

		/// <summary>Gets the line terminator string.</summary>
		public string LineTerminator { get; private set; } = "\n";

		/// <summary>Gets/sets the current insertion position.</summary>
		public int InsertionPosition { get; set; }

		/// <summary>Gets the start position of the snippet.</summary>
		public int StartPosition { get; private set; }

		/// <summary>Inserts text at the current position.</summary>
		public void InsertText(string text) { }

		/// <summary>Registers an active element.</summary>
		public void RegisterActiveElement(SnippetElement owner, IActiveElement element) { }

		/// <summary>Gets the active element for the given owner.</summary>
		public IActiveElement GetActiveElement(SnippetElement owner) => null;

		/// <summary>Gets all active elements.</summary>
		public IEnumerable<IActiveElement> ActiveElements => Array.Empty<IActiveElement>();

		/// <summary>Raises the <see cref="InsertionCompleted"/> event.</summary>
		public void RaiseInsertionCompleted(EventArgs e) { InsertionCompleted?.Invoke(this, e); }

		/// <summary>Raised when insertion is completed.</summary>
		public event EventHandler InsertionCompleted;

		/// <summary>Deactivates the insertion context.</summary>
		public void Deactivate(SnippetEventArgs e) { Deactivated?.Invoke(this, e); }

		/// <summary>Raised when the context is deactivated.</summary>
		public event EventHandler<SnippetEventArgs> Deactivated;

		/// <summary>Links a main element to bound elements.</summary>
		public void Link(ISegment mainElement, ISegment[] boundElements) { }
	}
}
