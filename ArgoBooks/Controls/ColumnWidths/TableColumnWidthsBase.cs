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

        var totalCurrentWidth = Columns.Values
            .Where(c => c.IsVisible)
            .Sum(c => c.CurrentWidth) + 24;

        if (_hasManualOverflow)
        {
            // Already in overflow state - check if we still need it
            if (width < totalCurrentWidth + 50)
            {
                // Still overflowing or close to it, maintain scroll state
                _availableWidth = width;
                MinimumTotalWidth = totalCurrentWidth;
                NeedsHorizontalScroll = true;
                return;
            }
            // Enough space now (with buffer), can reset overflow state
            _hasManualOverflow = false;
        }
        else if (totalCurrentWidth > width)
        {
            // Window resize caused overflow - enable scrolling
            _availableWidth = width;
            _hasManualOverflow = true;
            MinimumTotalWidth = totalCurrentWidth;
            NeedsHorizontalScroll = true;
            return;
        }

        _availableWidth = width;
        RecalculateWidths();
    }

    /// <summary>
    /// Adjusts a column width by a delta amount.
    /// </summary>
    /// <returns>The actual delta that was applied (may be less than requested due to constraints).</returns>
    public double ResizeColumn(string columnName, double delta)
    {
        if (!Columns.TryGetValue(columnName, out var col)) return 0;
        if (!col.IsVisible || col.IsFixed) return 0;
        if (Math.Abs(delta) < 0.5) return 0;

        var visibleColumns = ColumnOrder
            .Where(name => Columns.TryGetValue(name, out var c) && c.IsVisible)
            .ToList();

        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return 0;

        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();

        double totalCurrentWidth = visibleColumns.Sum(name => Columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 24;

        var newColWidth = col.CurrentWidth + delta;
        newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, newColWidth));
        var actualDelta = newColWidth - col.CurrentWidth;

        if (Math.Abs(actualDelta) < 0.5) return 0;

        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        // Always try to shrink columns to the right when expanding
        if (actualDelta > 0.5)
        {
            double shrinkNeeded = actualDelta;
            var shrinkableColumns = columnsToRight.Where(name => !Columns[name].IsFixed).ToList();
            foreach (var rightColName in shrinkableColumns)
            {
                var rightCol = Columns[rightColName];
                double availableShrink = rightCol.CurrentWidth - rightCol.MinWidth;
                if (availableShrink > 0.5)
                {
                    double actualShrink = Math.Min(shrinkNeeded, availableShrink);
                    rightCol.CurrentWidth -= actualShrink;
                    ApplyWidthToProperty(rightColName, rightCol.CurrentWidth);
                    shrinkNeeded -= actualShrink;
                    if (shrinkNeeded < 0.5) break;
                }
            }
        }

        // Check if we've exceeded max width
        double newTotalWidth = visibleColumns.Sum(name => Columns[name].CurrentWidth);
        if (newTotalWidth > maxTotalWidth)
        {
            _hasManualOverflow = true;
        }

        UpdateScrollState(visibleColumns);
        return actualDelta;
    }

    /// <summary>
    /// Updates the horizontal scroll state based on current column widths.
    /// </summary>
    protected void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => Columns[name].CurrentWidth) + 24;

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
                .Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 24;
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

            double minTotalWidth = visibleColumns.Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 24;

            if (_hasManualOverflow)
            {
                double totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 24;
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
            double availableForProportional = Math.Max(100, _availableWidth - fixedTotal - 24);

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

    /// <summary>
    /// Calculates the required width for an Actions column based on the number of icon buttons.
    /// Based on: 8px left margin + (n × 32px button) + ((n-1) × 4px spacing) + 4px right margin
    /// </summary>
    /// <param name="buttonCount">The number of 32x32 icon buttons in the Actions column.</param>
    /// <returns>The calculated width in pixels.</returns>
    public static double ActionsWidth(int buttonCount)
    {
        if (buttonCount <= 0) return 44;
        return 12 + (buttonCount * 32) + ((buttonCount - 1) * 4);
    }
}
