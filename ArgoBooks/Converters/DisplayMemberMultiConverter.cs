using System.Globalization;
using Avalonia.Data.Converters;

namespace ArgoBooks.Converters;

/// <summary>
/// Multi-value converter that returns the display text for an item using the DisplayMemberPath.
/// Values[0] = the item, Values[1] = the DisplayMemberPath string.
/// Uses reflection to get the property value specified by DisplayMemberPath.
/// </summary>
public class DisplayMemberMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return values.Count > 0 ? values[0]?.ToString() ?? string.Empty : string.Empty;

        var item = values[0];
        var displayMemberPath = values[1] as string;

        if (item == null)
            return string.Empty;

        if (string.IsNullOrEmpty(displayMemberPath))
            return item.ToString() ?? string.Empty;

        // Use reflection to get the property value
        var property = item.GetType().GetProperty(displayMemberPath);
        return property?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }
}
