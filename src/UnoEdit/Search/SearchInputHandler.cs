// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — stub for API parity.

using System;
using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Input handler that integrates the search panel into a TextArea.
	/// </summary>
	public class SearchInputHandler : TextAreaInputHandler
	{
		/// <summary>Creates a new SearchInputHandler.</summary>
		public SearchInputHandler(object textArea) : base(textArea) { }

		/// <summary>Raised when search options change.</summary>
		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;
	}
}
