using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a boolean to one of two fixed strings specified in the constructor.
/// </summary>
public class BoolToFixedStringConverter : IValueConverter
{
    private readonly string _trueValue;
    private readonly string _falseValue;

    public BoolToFixedStringConverter(string trueValue, string falseValue)
    {
        _trueValue = trueValue;
        _falseValue = falseValue;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? _trueValue : _falseValue;
        }
        return _falseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
