using System.Globalization;
using Avalonia.Data.Converters;
using ArgoBooks.Controls;

namespace ArgoBooks.Converters;

/// <summary>
/// Multi-value converter for sort indicator visibility.
/// Supports two modes:
/// 1. Parameter mode: [0] SortColumn, [1] SortDirection, Parameter: "ColumnName:Ascending" or "ColumnName:Descending"
/// 2. Values mode: [0] SortColumn, [1] SortDirection, [2] ColumnName, [3] Direction ("Ascending"/"Descending")
/// </summary>
public class SortIndicatorConverter : IMultiValueConverter
{
    public static readonly SortIndicatorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;

        var sortColumn = values[0] as string;
        var sortDirection = values[1] is SortDirection dir ? dir : SortDirection.None;

        string expectedColumn;
        SortDirection expectedDirection;

        // Mode 2: 4 values (SortColumn, SortDirection, ColumnName, DirectionString)
        if (values.Count >= 4 && values[2] is string colName && values[3] is string dirStr)
        {
            expectedColumn = colName;
            expectedDirection = dirStr switch
            {
                "Ascending" => SortDirection.Ascending,
                "Descending" => SortDirection.Descending,
                _ => SortDirection.None
            };
        }
        // Mode 1: Parameter string "ColumnName:Direction"
        else if (parameter is string param)
        {
            var parts = param.Split(':');
            if (parts.Length != 2)
                return false;

            expectedColumn = parts[0];
            expectedDirection = parts[1] switch
            {
                "Ascending" => SortDirection.Ascending,
                "Descending" => SortDirection.Descending,
                _ => SortDirection.None
            };
        }
        else
        {
            return false;
        }

        return sortColumn == expectedColumn && sortDirection == expectedDirection;
    }
}
