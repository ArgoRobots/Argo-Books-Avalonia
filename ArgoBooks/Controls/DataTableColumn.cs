using Avalonia.Markup.Xaml.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Sort direction for data table columns.
/// </summary>
public enum SortDirection
{
    None,
    Ascending,
    Descending
}

/// <summary>
/// Defines a column in a DataTable.
/// </summary>
public partial class DataTableColumn : ObservableObject
{
    /// <summary>
    /// Gets or sets the column header text.
    /// </summary>
    [ObservableProperty]
    private string _header = string.Empty;

    /// <summary>
    /// Gets or sets the property name to bind to.
    /// </summary>
    [ObservableProperty]
    private string _binding = string.Empty;

    /// <summary>
    /// Gets or sets the column width.
    /// </summary>
    [ObservableProperty]
    private double _width = double.NaN;

    /// <summary>
    /// Gets or sets the minimum column width.
    /// </summary>
    [ObservableProperty]
    private double _minWidth = 50;

    /// <summary>
    /// Gets or sets whether the column is sortable.
    /// </summary>
    [ObservableProperty]
    private bool _isSortable = true;

    /// <summary>
    /// Gets or sets whether the column is currently sorted.
    /// </summary>
    [ObservableProperty]
    private bool _isSorted;

    /// <summary>
    /// Gets or sets the current sort direction.
    /// </summary>
    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.None;

    /// <summary>
    /// Gets or sets the text alignment for the column.
    /// </summary>
    [ObservableProperty]
    private Avalonia.Layout.HorizontalAlignment _alignment = Avalonia.Layout.HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets a custom format string for the column value.
    /// </summary>
    [ObservableProperty]
    private string? _format;

    /// <summary>
    /// Gets or sets whether this column is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Gets or sets a custom cell template.
    /// </summary>
    public DataTemplate? CellTemplate { get; set; }

    /// <summary>
    /// Gets whether the column is sorted in descending order.
    /// </summary>
    public bool IsDescending => SortDirection == SortDirection.Descending;

    /// <summary>
    /// Gets the value from an item using the binding path.
    /// </summary>
    /// <param name="item">The data item.</param>
    /// <returns>The value at the binding path.</returns>
    public object? GetValue(object item)
    {
        if (string.IsNullOrEmpty(Binding))
            return item?.ToString();

        var property = item.GetType().GetProperty(Binding);
        var value = property?.GetValue(item);

        if (!string.IsNullOrEmpty(Format) && value is IFormattable formattable)
        {
            return formattable.ToString(Format, null);
        }

        return value;
    }

    /// <summary>
    /// Compares two items by this column's binding.
    /// </summary>
    /// <param name="x">First item.</param>
    /// <param name="y">Second item.</param>
    /// <returns>Comparison result.</returns>
    public int Compare(object? x, object? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var valueX = GetValue(x);
        var valueY = GetValue(y);

        if (valueX == null && valueY == null) return 0;
        if (valueX == null) return -1;
        if (valueY == null) return 1;

        // Handle IComparable
        if (valueX is IComparable comparableX)
        {
            return comparableX.CompareTo(valueY);
        }

        // Fall back to string comparison
        return string.Compare(valueX.ToString(), valueY.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Toggles the sort direction.
    /// </summary>
    public void ToggleSort()
    {
        SortDirection = SortDirection switch
        {
            SortDirection.None => SortDirection.Ascending,
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => SortDirection.None,
            _ => SortDirection.Ascending
        };

        IsSorted = SortDirection != SortDirection.None;
    }

    /// <summary>
    /// Clears the sort state.
    /// </summary>
    public void ClearSort()
    {
        SortDirection = SortDirection.None;
        IsSorted = false;
    }
}
