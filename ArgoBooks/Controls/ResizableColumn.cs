using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Defines a resizable column in a ResizableDataGrid.
/// </summary>
public partial class ResizableColumn : ObservableObject
{
    /// <summary>
    /// Gets or sets the column header text.
    /// </summary>
    [ObservableProperty]
    private string _header = string.Empty;

    /// <summary>
    /// Gets or sets the property path for data binding.
    /// </summary>
    [ObservableProperty]
    private string _binding = string.Empty;

    /// <summary>
    /// Gets or sets the current column width in pixels.
    /// </summary>
    [ObservableProperty]
    private double _width = 100;

    /// <summary>
    /// Gets or sets the minimum column width in pixels.
    /// </summary>
    [ObservableProperty]
    private double _minWidth = 50;

    /// <summary>
    /// Gets or sets the maximum column width in pixels.
    /// </summary>
    [ObservableProperty]
    private double _maxWidth = double.PositiveInfinity;

    /// <summary>
    /// Gets or sets the proportional width (star value).
    /// Used during auto-sizing to determine relative column widths.
    /// </summary>
    [ObservableProperty]
    private double _starWidth = 1.0;

    /// <summary>
    /// Gets or sets whether this column is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Gets or sets whether this column can be resized.
    /// </summary>
    [ObservableProperty]
    private bool _canResize = true;

    /// <summary>
    /// Gets or sets whether this column is fixed width (not proportional).
    /// Fixed columns maintain their width during auto-sizing.
    /// </summary>
    [ObservableProperty]
    private bool _isFixed;

    /// <summary>
    /// Gets or sets the text alignment for header and cells.
    /// </summary>
    [ObservableProperty]
    private Avalonia.Layout.HorizontalAlignment _alignment = Avalonia.Layout.HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets an optional format string for the column value.
    /// </summary>
    [ObservableProperty]
    private string? _format;

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
}
