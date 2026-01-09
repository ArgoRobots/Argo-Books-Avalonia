using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Represents a column definition with its sizing properties.
/// </summary>
public class ColumnDef
{
    public string Name { get; set; } = "";
    public double StarValue { get; set; } = 1.0;
    public double MinWidth { get; set; } = 50;
    public double MaxWidth { get; set; } = double.PositiveInfinity;
    public bool IsVisible { get; set; } = true;
    public bool IsFixed { get; set; }
    public double FixedWidth { get; set; } = 100;
    public double CurrentWidth { get; set; }
    public double PreferredWidth { get; set; } = 120;
    public double MeasuredContentWidth { get; set; }
}

/// <summary>
/// Abstract base class for managing column widths in a resizable table.
/// Provides common logic for proportional column distribution, resizing, and scrolling.
/// </summary>
public abstract partial class TableColumnWidthsBase : ObservableObject, ITableColumnWidths
{
    // Default to a reasonable desktop width to ensure proportional layout on first render
    // before OnTableSizeChanged provides the actual width
    private double _availableWidth = 1200;
    private bool _isUpdating;
    private bool _hasManualOverflow;

    /// <summary>
    /// Column definitions dictionary.
    /// </summary>
    protected readonly Dictionary<string, ColumnDef> Columns = new();

    /// <summary>
    /// Ordered list of column names for consistent iteration.
    /// </summary>
    protected string[] ColumnOrder { get; set; } = [];

    /// <summary>
    /// Maps column names to their width setter actions.
    /// </summary>
    protected readonly Dictionary<string, Action<double>> ColumnSetters = new();

    /// <summary>
    /// Gets the minimum total width required for all visible columns.
    /// </summary>
    [ObservableProperty]
    private double _minimumTotalWidth;

    /// <summary>
    /// Gets whether the table needs horizontal scrolling.
    /// </summary>
    [ObservableProperty]
    private bool _needsHorizontalScroll;

    /// <summary>
    /// Registers a column with its definition and width setter.
    /// </summary>
    protected void RegisterColumn(string name, ColumnDef def, Action<double> setter)
    {
        def.Name = name;
        Columns[name] = def;
        ColumnSetters[name] = setter;
    }

    /// <summary>
    /// Updates column visibility and recalculates widths.
    /// </summary>
    public void SetColumnVisibility(string columnName, bool isVisible)
    {
        if (Columns.TryGetValue(columnName, out var col))
        {
            col.IsVisible = isVisible;
            RecalculateWidths();
        }
    }

    /// <summary>
    /// Sets the available width for the table and recalculates column widths.
    /// </summary>
    public void SetAvailableWidth(double width)
    {
        if (Math.Abs(_availableWidth - width) < 1) return;

        if (_hasManualOverflow)
        {
            var totalWidth = Columns.Values
                .Where(c => c.IsVisible)
                .Sum(c => c.CurrentWidth) + 48;

            if (width < totalWidth + 50)
            {
                MinimumTotalWidth = totalWidth;
                NeedsHorizontalScroll = true;
                return;
            }
            _hasManualOverflow = false;
        }

        _availableWidth = width;
        RecalculateWidths();
    }

    /// <summary>
    /// Adjusts a column width by a delta amount.
    /// </summary>
    public void ResizeColumn(string columnName, double delta)
    {
        if (!Columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;
        if (Math.Abs(delta) < 0.5) return;

        var visibleColumns = ColumnOrder
            .Where(name => Columns.TryGetValue(name, out var c) && c.IsVisible)
            .ToList();

        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return;

        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();

        double totalCurrentWidth = visibleColumns.Sum(name => Columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 48;

        var newColWidth = col.CurrentWidth + delta;
        newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, newColWidth));
        var actualDelta = newColWidth - col.CurrentWidth;

        if (Math.Abs(actualDelta) < 0.5) return;

        if (totalCurrentWidth + actualDelta > maxTotalWidth)
        {
            _hasManualOverflow = true;
        }

        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        double shrinkNeeded = Math.Max(0, actualDelta - Math.Max(0, maxTotalWidth - totalCurrentWidth));

        if (shrinkNeeded > 0.5)
        {
            var shrinkableColumns = columnsToRight.Where(name => !Columns[name].IsFixed).ToList();
            foreach (var rightColName in shrinkableColumns)
            {
                var rightCol = Columns[rightColName];
                double availableShrink = rightCol.CurrentWidth - rightCol.MinWidth;
                double actualShrink = Math.Min(shrinkNeeded, availableShrink);
                rightCol.CurrentWidth -= actualShrink;
                ApplyWidthToProperty(rightColName, rightCol.CurrentWidth);
                shrinkNeeded -= actualShrink;
                if (shrinkNeeded < 0.5) break;
            }
        }

        UpdateScrollState(visibleColumns);
    }

    /// <summary>
    /// Updates the horizontal scroll state based on current column widths.
    /// </summary>
    protected void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => Columns[name].CurrentWidth) + 48;

        if (totalWidth > _availableWidth + 1)
        {
            _hasManualOverflow = true;
            NeedsHorizontalScroll = true;
            MinimumTotalWidth = totalWidth;
        }
        else
        {
            NeedsHorizontalScroll = false;
            MinimumTotalWidth = Columns.Values
                .Where(c => c.IsVisible)
                .Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;
        }
    }

    /// <summary>
    /// Gets the current width of a column.
    /// </summary>
    public double GetColumnWidth(string columnName)
    {
        if (Columns.TryGetValue(columnName, out var col))
        {
            return col.CurrentWidth > 0 ? col.CurrentWidth : col.MinWidth;
        }
        return 100;
    }

    /// <summary>
    /// Sets the width of a column directly.
    /// </summary>
    public void SetColumnWidth(string columnName, double width)
    {
        if (!Columns.TryGetValue(columnName, out var col)) return;
        if (col.IsFixed) return;

        width = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, width));
        col.CurrentWidth = width;
        ApplyWidthToProperty(columnName, width);
    }

    /// <summary>
    /// Gets the minimum width of a column.
    /// </summary>
    public double GetMinWidth(string columnName)
    {
        return Columns.TryGetValue(columnName, out var col) ? col.MinWidth : 50;
    }

    /// <summary>
    /// Registers a measured content width for a column.
    /// </summary>
    public void RegisterContentWidth(string columnName, double contentWidth)
    {
        if (Columns.TryGetValue(columnName, out var col))
        {
            var widthWithPadding = contentWidth + 20;
            if (widthWithPadding > col.MeasuredContentWidth)
            {
                col.MeasuredContentWidth = widthWithPadding;
            }
        }
    }

    /// <summary>
    /// Auto-sizes a column to fit its content or preferred width.
    /// </summary>
    public void AutoSizeColumn(string columnName)
    {
        if (!Columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;

        double targetWidth = col.MeasuredContentWidth > 0
            ? Math.Max(col.MinWidth, col.MeasuredContentWidth)
            : col.PreferredWidth;

        targetWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, targetWidth));

        var delta = targetWidth - col.CurrentWidth;
        if (Math.Abs(delta) > 0.5)
        {
            ResizeColumn(columnName, delta);
        }
    }

    /// <summary>
    /// Recalculates all column widths based on available space and visibility.
    /// </summary>
    public void RecalculateWidths()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var visibleColumns = Columns.Values.Where(c => c.IsVisible).ToList();
            if (visibleColumns.Count == 0) return;

            double minTotalWidth = visibleColumns.Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;

            if (_hasManualOverflow)
            {
                double totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 48;
                if (totalWidth + 50 > _availableWidth)
                {
                    MinimumTotalWidth = totalWidth;
                    NeedsHorizontalScroll = true;
                    return;
                }
                _hasManualOverflow = false;
            }

            MinimumTotalWidth = minTotalWidth;
            NeedsHorizontalScroll = _availableWidth < minTotalWidth;

            double fixedTotal = visibleColumns.Where(c => c.IsFixed).Sum(c => c.FixedWidth);
            double totalStars = visibleColumns.Where(c => !c.IsFixed).Sum(c => c.StarValue);
            double availableForProportional = Math.Max(100, _availableWidth - fixedTotal - 48);

            if (NeedsHorizontalScroll)
            {
                foreach (var col in visibleColumns)
                {
                    col.CurrentWidth = col.IsFixed ? col.FixedWidth : col.MinWidth;
                    ApplyWidthToProperty(col.Name, col.CurrentWidth);
                }
                return;
            }

            double widthPerStar = totalStars > 0 ? availableForProportional / totalStars : 0;

            foreach (var col in visibleColumns)
            {
                col.CurrentWidth = col.IsFixed
                    ? col.FixedWidth
                    : Math.Max(col.MinWidth, Math.Min(col.MaxWidth, col.StarValue * widthPerStar));
                ApplyWidthToProperty(col.Name, col.CurrentWidth);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Applies the width to the corresponding property using the registered setter.
    /// </summary>
    protected void ApplyWidthToProperty(string columnName, double width)
    {
        if (ColumnSetters.TryGetValue(columnName, out var setter))
        {
            setter(width);
        }
    }
}
