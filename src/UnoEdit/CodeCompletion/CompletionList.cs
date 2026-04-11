// Uno-native implementation of CompletionList.
// Acts as a listbox facade: holds completion items, manages filtering/selection,
// and issues InsertionRequested when the user commits an entry.
// On Uno the visual container is a CompletionListBox (ListView-based).
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// The listbox used inside the CompletionWindow, contains CompletionListBox.
	/// </summary>
	public class CompletionList : UserControl
	{
		bool isFiltering = true;

		/// <summary>
		/// If true the list is filtered to show only matching items (default).
		/// If false, old behavior: no filtering, starts-with search only.
		/// </summary>
		public bool IsFiltering
		{
			get => isFiltering;
			set => isFiltering = value;
		}

		/// <summary>Dependency property for <see cref="EmptyTemplate"/>.</summary>
		public static readonly DependencyProperty EmptyTemplateProperty =
			DependencyProperty.Register(nameof(EmptyTemplate), typeof(object),
				typeof(CompletionList), new PropertyMetadata(null));

		/// <summary>Content shown when the list is empty. Null means nothing is shown.</summary>
		public object EmptyTemplate
		{
			get => GetValue(EmptyTemplateProperty);
			set => SetValue(EmptyTemplateProperty, value);
		}

		/// <summary>Raised when the user has chosen an entry to insert.</summary>
		public event EventHandler InsertionRequested;

		/// <summary>Raises <see cref="InsertionRequested"/>.</summary>
		public void RequestInsertion(EventArgs e) => InsertionRequested?.Invoke(this, e);

		CompletionListBox listBox;

		/// <summary>Gets the inner CompletionListBox.</summary>
		public CompletionListBox ListBox => listBox;

		/// <summary>Gets the scroll viewer used in the list box.</summary>
		public ScrollViewer ScrollViewer => listBox?.scrollViewer;

		readonly ObservableCollection<ICompletionData> completionData = new();

		/// <summary>Collection to which completion data can be added.</summary>
		public IList<ICompletionData> CompletionData => completionData;

		/// <summary>Gets or sets the selected item.</summary>
		public ICompletionData SelectedItem
		{
			get => listBox?.SelectedItem as ICompletionData;
			set
			{
				if (listBox != null) listBox.SelectedItem = value;
			}
		}

		/// <summary>Scrolls <paramref name="item"/> into view.</summary>
		public void ScrollIntoView(ICompletionData item) => listBox?.ScrollIntoView(item);

		/// <summary>Raised when the selection changes.</summary>
		public event SelectionChangedEventHandler SelectionChanged;

		// Avoid re-filtering if the text hasn't changed.
		string currentText;
		ObservableCollection<ICompletionData> currentList;

		/// <summary>
		/// Selects the best match and (if <see cref="IsFiltering"/> is true) filters the list.
		/// </summary>
		public void SelectItem(string text)
		{
			if (text == currentText) return;
			if (IsFiltering)
				SelectItemFiltering(text);
			else
				SelectItemWithStart(text);
			currentText = text;
		}

		void SelectItemFiltering(string query)
		{
			var listToFilter = (currentList != null
								&& !string.IsNullOrEmpty(currentText)
								&& !string.IsNullOrEmpty(query)
								&& query.StartsWith(currentText, StringComparison.Ordinal))
				? currentList : completionData;

			var newList = new ObservableCollection<ICompletionData>(
				listToFilter
					.Where(item => item.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
					.OrderByDescending(item => MatchQuality(item.Text, query)));

			if (listBox != null)
				listBox.ItemsSource = newList;

			currentList = newList;

			if (newList.Count > 0)
				listBox?.SelectIndex(0);
		}

		void SelectItemWithStart(string query)
		{
			if (listBox == null) return;
			for (int i = 0; i < completionData.Count; i++)
			{
				if (completionData[i].Text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
				{
					listBox.SelectIndex(i);
					return;
				}
			}
		}

		static double MatchQuality(string itemText, string query)
		{
			// Exact match gets highest priority.
			if (itemText.Equals(query, StringComparison.OrdinalIgnoreCase)) return 3;
			// Starts-with match.
			if (itemText.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 2;
			// Contains match.
			return 1;
		}

		/// <summary>Handles a key press while focus is still on the text editor.</summary>
		public void HandleKey(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (listBox == null) return;
			switch (e.Key)
			{
				case Windows.System.VirtualKey.Down:
					e.Handled = true;
					listBox.SelectIndex(listBox.SelectedIndex + 1);
					break;
				case Windows.System.VirtualKey.Up:
					e.Handled = true;
					listBox.SelectIndex(listBox.SelectedIndex - 1);
					break;
				case Windows.System.VirtualKey.PageDown:
					e.Handled = true;
					listBox.SelectIndex(listBox.SelectedIndex + listBox.VisibleItemCount);
					break;
				case Windows.System.VirtualKey.PageUp:
					e.Handled = true;
					listBox.SelectIndex(listBox.SelectedIndex - listBox.VisibleItemCount);
					break;
				case Windows.System.VirtualKey.Home:
					e.Handled = true;
					listBox.SelectIndex(0);
					break;
				case Windows.System.VirtualKey.End:
					e.Handled = true;
					listBox.SelectIndex(listBox.Items.Count - 1);
					break;
				case Windows.System.VirtualKey.Tab:
				case Windows.System.VirtualKey.Enter:
					e.Handled = true;
					RequestInsertion(e);
					break;
			}
		}

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			if (listBox == null)
			{
				listBox = new CompletionListBox { ItemsSource = completionData };
				listBox.SelectionChanged += ListBox_SelectionChanged;
				Content = listBox;
			}
		}

		/// <summary>Creates and initialises the inner list box (call after constructing).</summary>
		public CompletionList()
		{
			listBox = new CompletionListBox { ItemsSource = completionData };
			listBox.SelectionChanged += ListBox_SelectionChanged;
			Content = listBox;
		}

		void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> SelectionChanged?.Invoke(this, e);
	}
}
