using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that returns true if the bound value equals the CompareValue.
/// Optionally returns TrueValue/FalseValue instead of bool.
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// The value to compare against.
    /// </summary>
    public string? CompareValue { get; set; }

    /// <summary>
    /// Optional value to return when the comparison is true.
    /// </summary>
    public object? TrueValue { get; set; }

    /// <summary>
    /// Optional value to return when the comparison is false.
    /// </summary>
    public object? FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isEqual = false;
        if (value is string stringValue)
        {
            isEqual = string.Equals(stringValue, CompareValue, StringComparison.OrdinalIgnoreCase);
        }

        // If TrueValue/FalseValue are set, return those instead of bool
        if (TrueValue != null || FalseValue != null)
        {
            var result = isEqual ? TrueValue : FalseValue;

            // Handle Thickness conversion for BorderThickness
            if (targetType == typeof(Thickness) && result is string thicknessStr)
            {
                if (double.TryParse(thicknessStr, out var thickness))
                {
                    return new Thickness(thickness);
                }
            }

            return result;
        }

        return isEqual;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns true if the bound value equals the parameter value.
/// Used for RadioButton IsChecked binding where the parameter is the option value.
/// </summary>
public class EqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return true;
        if (value == null || parameter == null)
            return false;
        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked)
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

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns PrimaryBrush if value equals CompareValue, otherwise BorderBrush.
/// </summary>
public class ThemeBorderBrushConverter : IValueConverter
{
    /// <summary>
    /// The value to compare against.
    /// </summary>
    public string? CompareValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isSelected = false;
        if (value is string stringValue)
        {
            isSelected = string.Equals(stringValue, CompareValue, StringComparison.OrdinalIgnoreCase);
        }

        // Get the appropriate brush from Application resources
        if (Application.Current?.Resources != null)
        {
            var resourceKey = isSelected ? "PrimaryBrush" : "BorderBrush";
            if (Application.Current.Resources.TryGetResource(resourceKey, Application.Current.ActualThemeVariant, out var resource))
            {
                return resource;
            }
        }

        // Fallback colors
        return isSelected
            ? new SolidColorBrush(Color.Parse("#3B82F6"))
            : new SolidColorBrush(Color.Parse("#E5E7EB"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
