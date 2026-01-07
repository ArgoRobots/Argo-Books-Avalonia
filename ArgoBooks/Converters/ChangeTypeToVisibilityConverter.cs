using System.Globalization;
using ArgoBooks.ViewModels;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Converter that converts a ChangeType enum to a boolean for visibility.
/// </summary>
public class ChangeTypeToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the target change type to match.
    /// </summary>
    public ChangeType TargetType { get; set; } = ChangeType.Modified;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ChangeType changeType && changeType == TargetType;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
