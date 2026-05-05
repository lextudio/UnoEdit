using System.ComponentModel;
using System.Reflection;

namespace UnoEdit.PropertyGrid;

public class PropertyItemViewModel : INotifyPropertyChanged
{
    private readonly object _instance;
    private readonly PropertyInfo _property;
    private bool _isVisible = true;
    private object? _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string PropertyName { get; }
    public string? Category { get; }
    public Type PropertyType { get; }
    public bool CanWrite { get; }

    public object? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                if (CanWrite)
                {
                    try
                    {
                        _property.SetValue(_instance, value);
                        Console.WriteLine($"[PropertyGrid] Set {PropertyName} = {value}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PropertyGrid] Error setting {PropertyName}: {ex.Message}");
                    }
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }
    }

    public PropertyItemViewModel(object instance, PropertyInfo property)
    {
        _instance = instance;
        _property = property;
        PropertyName = property.Name;
        PropertyType = property.PropertyType;
        CanWrite = property.CanWrite;

        var catAttr = property.GetCustomAttribute<CategoryAttribute>();
        Category = catAttr?.Category;

        try
        {
            _value = property.GetValue(instance);
        }
        catch { }

        if (instance is INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == property.Name)
                {
                    try
                    {
                        _value = property.GetValue(_instance);
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                    catch { }
                }
            };
        }
    }
}
