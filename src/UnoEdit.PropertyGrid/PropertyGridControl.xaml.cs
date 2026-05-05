using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;

namespace UnoEdit.PropertyGrid;

public sealed partial class PropertyGridControl : UserControl
{
    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(
            nameof(SelectedObject),
            typeof(object),
            typeof(PropertyGridControl),
            new PropertyMetadata(null, OnSelectedObjectChanged));

    private ObservableCollection<PropertyItemViewModel> _properties = new();
    private object? _selectedObject;

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public PropertyGridControl()
    {
        this.InitializeComponent();
        PropertiesItemsControl.ItemsSource = _properties;
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyGridControl)d;
        control.RefreshProperties((object?)e.NewValue);
    }

    private void RefreshProperties(object? obj)
    {
        _selectedObject = obj;
        _properties.Clear();

        if (obj == null)
            return;

        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name)
            .ToList();

        foreach (var prop in properties)
        {
            _properties.Add(new PropertyItemViewModel(obj, prop));
        }

        PopulateCategories();
    }

    private void PopulateCategories()
    {
        var categories = _properties
            .Select(p => p.Category ?? "Uncategorized")
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        CategoryComboBox.Items.Clear();
        CategoryComboBox.Items.Add("All Properties");
        foreach (var cat in categories)
        {
            CategoryComboBox.Items.Add(cat);
        }
        CategoryComboBox.SelectedIndex = 0;
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = (string?)CategoryComboBox.SelectedItem;
        if (selected == null || selected == "All Properties")
        {
            foreach (var prop in _properties)
                prop.IsVisible = true;
        }
        else
        {
            foreach (var prop in _properties)
                prop.IsVisible = prop.Category == selected;
        }
    }
}
