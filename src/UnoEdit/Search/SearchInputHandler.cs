// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// Ported to UnoEdit — stub for API parity.

using System;
using System.Reflection;
using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Input handler that integrates the search panel into a TextArea.
	/// </summary>
	public class SearchInputHandler : TextAreaInputHandler
	{
		readonly object searchPanel;

		/// <summary>Creates a new SearchInputHandler.</summary>
		public SearchInputHandler(object textArea) : base(textArea)
		{
			searchPanel = ResolveSearchPanel(textArea);
			if (searchPanel != null) {
				var eventInfo = searchPanel.GetType().GetEvent("SearchOptionsChanged", BindingFlags.Public | BindingFlags.Instance);
				if (eventInfo != null) {
					var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, nameof(OnSearchPanelOptionsChanged));
					eventInfo.AddEventHandler(searchPanel, handler);
				}
			}
		}

		/// <summary>Raised when search options change.</summary>
		public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

		void OnSearchPanelOptionsChanged(object sender, SearchOptionsChangedEventArgs e)
		{
			SearchOptionsChanged?.Invoke(this, e);
		}

		static object ResolveSearchPanel(object textArea)
		{
			if (textArea == null)
				return null;

			var prop = textArea.GetType().GetProperty("SearchPanel", BindingFlags.Public | BindingFlags.Instance);
			if (prop != null)
				return prop.GetValue(textArea);

			if (textArea is Microsoft.UI.Xaml.FrameworkElement fe) {
				var current = fe;
				while (current != null) {
					var p = current.GetType().GetProperty("SearchPanel", BindingFlags.Public | BindingFlags.Instance);
					if (p?.GetValue(current) is object panel)
						return panel;
					current = current.Parent as Microsoft.UI.Xaml.FrameworkElement;
				}
			}

			return null;
		}
	}
}
