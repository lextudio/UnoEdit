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
            PropertyGridLogger.Log($"VM.Value setter [{PropertyName}]: incoming={value}, current=_value={_value}, equal={Equals(_value, value)}");
            if (!Equals(_value, value))
            {
                _value = value;
                if (CanWrite)
                {
                    try
                    {
                        _property.SetValue(_instance, value);
                        PropertyGridLogger.Log($"VM.Value setter [{PropertyName}]: SetValue succeeded, reading back={_property.GetValue(_instance)}");
                    }
                    catch (Exception ex)
                    {
                        PropertyGridLogger.Log($"VM.Value setter [{PropertyName}]: SetValue FAILED: {ex.Message}");
                    }
                }
                PropertyGridLogger.Log($"VM.Value setter [{PropertyName}]: firing PropertyChanged(Value)");
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                PropertyGridLogger.Log($"VM.Value setter [{PropertyName}]: PropertyChanged(Value) done");
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
            PropertyGridLogger.Log($"VM [{property.Name}]: subscribing to INPC on {instance.GetType().Name}");
            inpc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == property.Name)
                {
                    try
                    {
                        var newVal = property.GetValue(_instance);
                        PropertyGridLogger.Log($"VM [{property.Name}]: INPC back-sync fired, newVal={newVal}, old _value={_value}");
                        _value = newVal;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                    catch (Exception ex)
                    {
                        PropertyGridLogger.Log($"VM [{property.Name}]: INPC back-sync exception: {ex.Message}");
                    }
                }
            };
        }
    }
}
