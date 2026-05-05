using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace UnoEdit.PropertyGrid;

public sealed partial class PropertyEditorControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PropertyItemViewModel),
            typeof(PropertyEditorControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public event PropertyChangedEventHandler? PropertyChanged;

    public PropertyItemViewModel? ViewModel
    {
        get => (PropertyItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PropertyEditorControl()
    {
        this.InitializeComponent();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PropertyEditorControl)d;
        if (e.OldValue is PropertyItemViewModel oldVm)
            oldVm.PropertyChanged -= control.OnViewModelPropertyChanged;
        if (e.NewValue is PropertyItemViewModel newVm)
            newVm.PropertyChanged += control.OnViewModelPropertyChanged;
        control.UpdateEditorVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PropertyItemViewModel.Value))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoolValue)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberValue)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnumValue)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReadOnlyValue)));
        }
    }

    private void UpdateEditorVisibility()
    {
        if (ViewModel == null)
            return;

        var type = ViewModel.PropertyType;
        var isNullable = Nullable.GetUnderlyingType(type) != null;
        var baseType = isNullable ? Nullable.GetUnderlyingType(type)! : type;

        if (baseType == typeof(bool))
            ShowBoolEditor();
        else if (baseType == typeof(string))
            ShowStringEditor();
        else if (baseType == typeof(int) || baseType == typeof(double) || baseType == typeof(float))
            ShowNumberEditor();
        else if (baseType.IsEnum)
            ShowEnumEditor(baseType);
        else
            ShowReadOnlyEditor();
    }

    private void ShowBoolEditor()
    {
        BoolEditor.Visibility = Visibility.Visible;
        StringEditor.Visibility = Visibility.Collapsed;
        NumberEditor.Visibility = Visibility.Collapsed;
        EnumEditor.Visibility = Visibility.Collapsed;
        ReadOnlyEditor.Visibility = Visibility.Collapsed;
    }

    private void ShowStringEditor()
    {
        BoolEditor.Visibility = Visibility.Collapsed;
        StringEditor.Visibility = Visibility.Visible;
        NumberEditor.Visibility = Visibility.Collapsed;
        EnumEditor.Visibility = Visibility.Collapsed;
        ReadOnlyEditor.Visibility = Visibility.Collapsed;
    }

    private void ShowNumberEditor()
    {
        BoolEditor.Visibility = Visibility.Collapsed;
        StringEditor.Visibility = Visibility.Collapsed;
        NumberEditor.Visibility = Visibility.Visible;
        EnumEditor.Visibility = Visibility.Collapsed;
        ReadOnlyEditor.Visibility = Visibility.Collapsed;
    }

    private void ShowEnumEditor(Type enumType)
    {
        EnumEditor.ItemsSource = Enum.GetValues(enumType).Cast<object>().ToList();
        BoolEditor.Visibility = Visibility.Collapsed;
        StringEditor.Visibility = Visibility.Collapsed;
        NumberEditor.Visibility = Visibility.Collapsed;
        EnumEditor.Visibility = Visibility.Visible;
        ReadOnlyEditor.Visibility = Visibility.Collapsed;
    }

    private void ShowReadOnlyEditor()
    {
        BoolEditor.Visibility = Visibility.Collapsed;
        StringEditor.Visibility = Visibility.Collapsed;
        NumberEditor.Visibility = Visibility.Collapsed;
        EnumEditor.Visibility = Visibility.Collapsed;
        ReadOnlyEditor.Visibility = Visibility.Visible;
    }

    #region Editor bindings

    public bool BoolValue
    {
        get => ViewModel?.Value is bool b && b;
        set
        {
            if (ViewModel != null)
            {
                Console.WriteLine($"[PropertyEditor] BoolValue setter: {ViewModel.PropertyName} = {value}");
                ViewModel.Value = value;
            }
        }
    }

    public string StringValue
    {
        get => ViewModel?.Value as string ?? "";
        set
        {
            if (ViewModel != null)
                ViewModel.Value = value;
        }
    }

    public string NumberValue
    {
        get => ViewModel?.Value?.ToString() ?? "";
        set
        {
            if (ViewModel == null || !ViewModel.CanWrite)
                return;

            var type = ViewModel.PropertyType;
            var isNullable = Nullable.GetUnderlyingType(type) != null;
            var baseType = isNullable ? Nullable.GetUnderlyingType(type)! : type;

            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    ViewModel.Value = isNullable ? null : Activator.CreateInstance(baseType);
                    return;
                }

                ViewModel.Value = baseType == typeof(int)
                    ? int.Parse(value)
                    : baseType == typeof(double)
                    ? double.Parse(value)
                    : float.Parse(value);
            }
            catch { }
        }
    }

    public object? EnumValue
    {
        get => ViewModel?.Value;
        set
        {
            if (ViewModel != null)
                ViewModel.Value = value;
        }
    }

    public object? EnumValues => null;

    public string ReadOnlyValue
    {
        get
        {
            var value = ViewModel?.Value;
            if (value == null)
                return "(null)";
            if (value is bool b)
                return b.ToString();
            return value.ToString() ?? "(null)";
        }
    }

    public Visibility BoolEditorVisibility => ViewModel?.PropertyType == typeof(bool) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility StringEditorVisibility => ViewModel?.PropertyType == typeof(string) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NumberEditorVisibility => (ViewModel?.PropertyType == typeof(int) || ViewModel?.PropertyType == typeof(double) || ViewModel?.PropertyType == typeof(float)) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EnumEditorVisibility => ViewModel?.PropertyType?.IsEnum == true ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ReadOnlyEditorVisibility => BoolEditorVisibility == Visibility.Visible || StringEditorVisibility == Visibility.Visible || NumberEditorVisibility == Visibility.Visible || EnumEditorVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    #endregion
}
