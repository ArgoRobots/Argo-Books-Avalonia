using System.Globalization;
using Avalonia.Data.Converters;
using ArgoBooks.Controls;

namespace ArgoBooks.Converters;

/// <summary>
/// Multi-value converter for sort indicator visibility.
/// Expects: [0] SortColumn, [1] SortDirection, Parameter: "ColumnName:Ascending" or "ColumnName:Descending"
/// </summary>
public class SortIndicatorConverter : IMultiValueConverter
{
    public static readonly SortIndicatorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || parameter is not string param)
            return false;

        var sortColumn = values[0] as string;
        var sortDirection = values[1] is SortDirection dir ? dir : SortDirection.None;

        var parts = param.Split(':');
        if (parts.Length != 2)
            return false;

        var expectedColumn = parts[0];
        var expectedDirection = parts[1] switch
        {
            "Ascending" => SortDirection.Ascending,
            "Descending" => SortDirection.Descending,
            _ => SortDirection.None
        };

        return sortColumn == expectedColumn && sortDirection == expectedDirection;
    }
}
