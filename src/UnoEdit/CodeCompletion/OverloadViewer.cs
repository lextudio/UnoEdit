// Uno-native OverloadViewer and DropDownButton.
// OverloadViewer shows an IOverloadProvider with Up/Down navigation buttons.
// DropDownButton is a Button that shows a popup containing DropDownContent.
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// A control that shows the current overload index text between two navigation buttons.
	/// </summary>
	public class OverloadViewer : UserControl
	{
		readonly Button upButton;
		readonly Button downButton;
		readonly TextBlock textBlock;

		/// <summary>Creates a new <see cref="OverloadViewer"/>.</summary>
		public OverloadViewer()
		{
			upButton   = new Button { Content = "▲", Padding = new Thickness(4, 0, 4, 0) };
			textBlock  = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 4, 0) };
			downButton = new Button { Content = "▼", Padding = new Thickness(4, 0, 4, 0) };

			var panel = new StackPanel { Orientation = Orientation.Horizontal };
			panel.Children.Add(upButton);
			panel.Children.Add(textBlock);
			panel.Children.Add(downButton);
			Content = panel;

			upButton.Click   += (_, _) => ChangeIndex(-1);
			downButton.Click += (_, _) => ChangeIndex(+1);
		}

		/// <summary>Dependency property for <see cref="Text"/>.</summary>
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(string),
				typeof(OverloadViewer), new PropertyMetadata(null, OnTextChanged));

		static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			=> ((OverloadViewer)d).textBlock.Text = e.NewValue as string;

		/// <summary>Gets/Sets the text shown between the navigation buttons.</summary>
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		/// <summary>Dependency property for <see cref="Provider"/>.</summary>
		public static readonly DependencyProperty ProviderProperty =
			DependencyProperty.Register(nameof(Provider), typeof(IOverloadProvider),
				typeof(OverloadViewer), new PropertyMetadata(null, OnProviderChanged));

		static void OnProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var viewer = (OverloadViewer)d;
			if (e.OldValue is System.ComponentModel.INotifyPropertyChanged oldProvider)
				oldProvider.PropertyChanged -= viewer.Provider_PropertyChanged;
			if (e.NewValue is System.ComponentModel.INotifyPropertyChanged newProvider)
				newProvider.PropertyChanged += viewer.Provider_PropertyChanged;
			viewer.UpdateText();
		}

		void Provider_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
			=> UpdateText();

		void UpdateText()
		{
			var p = Provider;
			Text = p?.CurrentIndexText ?? string.Empty;
		}

		/// <summary>Gets/Sets the overload provider.</summary>
		public IOverloadProvider Provider
		{
			get => (IOverloadProvider)GetValue(ProviderProperty);
			set => SetValue(ProviderProperty, value);
		}

		/// <summary>
		/// Changes the selected index by <paramref name="relativeIndexChange"/> (+1 or -1).
		/// Wraps around at both ends.
		/// </summary>
		public void ChangeIndex(int relativeIndexChange)
		{
			var p = Provider;
			if (p == null) return;
			int newIndex = p.SelectedIndex + relativeIndexChange;
			if (newIndex < 0) newIndex = p.Count - 1;
			if (newIndex >= p.Count) newIndex = 0;
			p.SelectedIndex = newIndex;
		}

		/// <inheritdoc/>
		protected override void OnApplyTemplate() => base.OnApplyTemplate();
	}

	/// <summary>
	/// A button that opens a popup to show its <see cref="DropDownContent"/> when clicked.
	/// </summary>
	public class DropDownButton : Button
	{
		readonly Popup dropDownPopup;

		/// <summary>Dependency property for <see cref="DropDownContent"/>.</summary>
		public static readonly DependencyProperty DropDownContentProperty =
			DependencyProperty.Register(nameof(DropDownContent), typeof(object),
				typeof(DropDownButton), new PropertyMetadata(null, OnDropDownContentChanged));

		static void OnDropDownContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var btn = (DropDownButton)d;
			if (btn.dropDownPopup.Child is ContentPresenter cp)
				cp.Content = e.NewValue;
			else
				btn.dropDownPopup.Child = new ContentPresenter { Content = e.NewValue };
		}

		/// <summary>Dependency property for <see cref="IsDropDownContentOpen"/>.</summary>
		public static readonly DependencyProperty IsDropDownContentOpenProperty =
			DependencyProperty.Register(nameof(IsDropDownContentOpen), typeof(bool),
				typeof(DropDownButton), new PropertyMetadata(false, OnIsOpenChanged));

		static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			=> ((DropDownButton)d).dropDownPopup.IsOpen = (bool)e.NewValue;

		/// <summary>Creates a new <see cref="DropDownButton"/>.</summary>
		public DropDownButton()
		{
			dropDownPopup = new Popup { IsLightDismissEnabled = true };
			dropDownPopup.Closed += (_, _) => IsDropDownContentOpen = false;
			Click += (_, _) => IsDropDownContentOpen = !IsDropDownContentOpen;
		}

		/// <summary>Gets/Sets the content shown in the dropdown popup.</summary>
		public object DropDownContent
		{
			get => GetValue(DropDownContentProperty);
			set => SetValue(DropDownContentProperty, value);
		}

		/// <summary>Gets/Sets whether the dropdown popup is open.</summary>
		public bool IsDropDownContentOpen
		{
			get => (bool)GetValue(IsDropDownContentOpenProperty);
			set => SetValue(IsDropDownContentOpenProperty, value);
		}
	}
}
