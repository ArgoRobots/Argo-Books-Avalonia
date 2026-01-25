using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that returns true if the bound value equals the parameter value.
/// Used for RadioButton IsChecked binding where the parameter is the option value.
/// </summary>
public class EqualityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return true;
        if (value == null || parameter == null)
            return false;
        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool and true)
            return parameter;
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Converter that returns true if the bound integer value equals the parameter integer.
/// Handles string parameter conversion for XAML compatibility.
/// </summary>
public class EqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        // Handle integer comparison with string parameter
        if (value is int intValue && parameter is string strParam)
        {
            if (int.TryParse(strParam, out var paramInt))
                return intValue == paramInt;
        }

        return value.Equals(parameter);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
