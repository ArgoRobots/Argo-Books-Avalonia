using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for a resizable table.
/// Automatically distributes available width proportionally among visible columns.
/// </summary>
public partial class TableColumnWidths : ObservableObject
{
    private double _availableWidth = 800;
    private bool _isUpdating;
    private bool _hasManualOverflow; // True when user resize caused columns to exceed available width

    // Column definitions with star values (proportion weights)
    private readonly Dictionary<string, ColumnDef> _columns = new();

    // Ordered list of all column names for consistent iteration
    private readonly string[] _columnOrder = { "Id", "Accountant", "Product", "Supplier", "Date", "Quantity",
        "UnitPrice", "Amount", "Tax", "Shipping", "Discount", "Total", "Receipt", "Status", "Actions" };

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
        public double PreferredWidth { get; set; } = 120; // Default comfortable width for auto-size
        public double MeasuredContentWidth { get; set; } // Max measured content width
    }

    #region Column Width Properties

    [ObservableProperty]
    private double _idColumnWidth = 100;

    [ObservableProperty]
    private double _accountantColumnWidth = 80;

    [ObservableProperty]
    private double _productColumnWidth = 150;

    [ObservableProperty]
    private double _supplierColumnWidth = 100;

    [ObservableProperty]
    private double _dateColumnWidth = 90;

    [ObservableProperty]
    private double _quantityColumnWidth = 50;

    [ObservableProperty]
    private double _unitPriceColumnWidth = 80;

    [ObservableProperty]
    private double _amountColumnWidth = 80;

    [ObservableProperty]
    private double _taxColumnWidth = 60;

    [ObservableProperty]
    private double _shippingColumnWidth = 70;

    [ObservableProperty]
    private double _discountColumnWidth = 70;

    [ObservableProperty]
    private double _totalColumnWidth = 80;

    [ObservableProperty]
    private double _receiptColumnWidth = 60;

    [ObservableProperty]
    private double _statusColumnWidth = 90;

    [ObservableProperty]
    private double _actionsColumnWidth = 160;

    /// <summary>
    /// Gets the minimum total width required for all visible columns.
    /// </summary>
    [ObservableProperty]
    private double _minimumTotalWidth = 0;

    /// <summary>
    /// Gets whether the table needs horizontal scrolling (available width less than minimum).
    /// </summary>
    [ObservableProperty]
    private bool _needsHorizontalScroll = false;

    #endregion

    public TableColumnWidths()
    {
        InitializeColumns();
    }

    private void InitializeColumns()
    {
        _columns["Id"] = new ColumnDef { Name = "Id", StarValue = 1.2, MinWidth = 60, PreferredWidth = 100 };
        _columns["Accountant"] = new ColumnDef { Name = "Accountant", StarValue = 1.0, MinWidth = 70, PreferredWidth = 120 };
        _columns["Product"] = new ColumnDef { Name = "Product", StarValue = 1.5, MinWidth = 100, PreferredWidth = 200 };
        _columns["Supplier"] = new ColumnDef { Name = "Supplier", StarValue = 1.2, MinWidth = 80, PreferredWidth = 150 };
        _columns["Date"] = new ColumnDef { Name = "Date", StarValue = 1.0, MinWidth = 80, PreferredWidth = 110 };
        _columns["Quantity"] = new ColumnDef { Name = "Quantity", StarValue = 0.5, MinWidth = 40, PreferredWidth = 70 };
        _columns["UnitPrice"] = new ColumnDef { Name = "UnitPrice", StarValue = 0.8, MinWidth = 60, PreferredWidth = 100 };
        _columns["Amount"] = new ColumnDef { Name = "Amount", StarValue = 0.8, MinWidth = 60, PreferredWidth = 100 };
        _columns["Tax"] = new ColumnDef { Name = "Tax", StarValue = 0.6, MinWidth = 50, PreferredWidth = 80 };
        _columns["Shipping"] = new ColumnDef { Name = "Shipping", StarValue = 0.7, MinWidth = 60, PreferredWidth = 90 };
        _columns["Discount"] = new ColumnDef { Name = "Discount", StarValue = 0.7, MinWidth = 60, PreferredWidth = 90 };
        _columns["Total"] = new ColumnDef { Name = "Total", StarValue = 0.8, MinWidth = 70, PreferredWidth = 110 };
        _columns["Receipt"] = new ColumnDef { Name = "Receipt", StarValue = 0.5, MinWidth = 50, PreferredWidth = 80 };
        _columns["Status"] = new ColumnDef { Name = "Status", StarValue = 0.9, MinWidth = 80, PreferredWidth = 110 };
        _columns["Actions"] = new ColumnDef { Name = "Actions", IsFixed = true, FixedWidth = 160, MinWidth = 160 };
    }

    /// <summary>
    /// Updates column visibility and recalculates widths.
    /// </summary>
    public void SetColumnVisibility(string columnName, bool isVisible)
    {
        if (_columns.TryGetValue(columnName, out var col))
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

        // If in manual overflow mode, don't update _availableWidth unless window is genuinely bigger
        if (_hasManualOverflow)
        {
            var totalWidth = _columns.Values
                .Where(c => c.IsVisible)
                .Sum(c => c.CurrentWidth) + 48;

            // Only exit overflow if width is significantly larger than content
            // Use large tolerance because Grid may expand to MinWidth during resize
            if (width < totalWidth + 50)
            {
                // Keep scrolling, don't update _availableWidth
                MinimumTotalWidth = totalWidth;
                NeedsHorizontalScroll = true;
                return;
            }
            // Window is now genuinely bigger than content - clear overflow
            _hasManualOverflow = false;
        }

        // Only update _availableWidth after passing overflow checks
        _availableWidth = width;
        RecalculateWidths();
    }

    /// <summary>
    /// Adjusts a column width by a delta amount (from resize drag).
    /// Redistributes the width change to columns on the right.
    /// </summary>
    public void ResizeColumn(string columnName, double delta)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;
        if (Math.Abs(delta) < 0.5) return;

        // Get ordered list of visible columns (including fixed)
        var visibleColumns = _columnOrder
            .Where(name => _columns.TryGetValue(name, out var c) && c.IsVisible)
            .ToList();

        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return;

        // Get all columns to the right (including fixed columns like Actions)
        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();

        // Calculate current total and available space
        double totalCurrentWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 48; // Subtract padding
        double extraSpace = Math.Max(0, maxTotalWidth - totalCurrentWidth);

        // Calculate how much the right columns can shrink (only non-fixed columns)
        double shrinkableWidth = columnsToRight
            .Where(name => !_columns[name].IsFixed)
            .Sum(name => _columns[name].CurrentWidth - _columns[name].MinWidth);

        // Calculate new width for the resized column
        var newColWidth = col.CurrentWidth + delta;
        newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, newColWidth));
        var actualDelta = newColWidth - col.CurrentWidth;

        if (Math.Abs(actualDelta) < 0.5) return;

        // Check if this resize will cause overflow BEFORE applying changes
        // This prevents RecalculateWidths from resetting columns during binding updates
        double projectedTotal = totalCurrentWidth + actualDelta;
        if (projectedTotal > maxTotalWidth)
        {
            _hasManualOverflow = true;
        }

        // Apply the change to the resized column
        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        // Calculate how much we need to shrink from the right columns
        // First use up extra space, then shrink right columns
        double shrinkNeeded = Math.Max(0, actualDelta - extraSpace);

        if (shrinkNeeded < 0.5)
        {
            UpdateScrollState(visibleColumns);
            return;
        }

        // Distribute the shrink to columns on the right (only non-fixed columns)
        var shrinkableColumns = columnsToRight.Where(name => !_columns[name].IsFixed).ToList();
        if (shrinkableColumns.Count == 0)
        {
            UpdateScrollState(visibleColumns);
            return;
        }

        // Multi-pass distribution: keep redistributing until all shrink is absorbed
        // or all columns are at their minimum
        double remainingShrink = shrinkNeeded;
        const int maxIterations = 10; // Safety limit

        for (int iteration = 0; iteration < maxIterations && remainingShrink > 0.5; iteration++)
        {
            // Find columns that can still shrink
            var canShrink = shrinkableColumns
                .Where(name => _columns[name].CurrentWidth > _columns[name].MinWidth + 0.5)
                .ToList();

            if (canShrink.Count == 0) break; // All columns at minimum

            // Calculate total width available for proportional distribution
            double shrinkPoolWidth = canShrink.Sum(name => _columns[name].CurrentWidth);
            if (shrinkPoolWidth < 0.5) break;

            double shrinkAppliedThisPass = 0;

            foreach (var rightColName in canShrink)
            {
                var rightCol = _columns[rightColName];
                double proportion = rightCol.CurrentWidth / shrinkPoolWidth;
                double targetShrink = remainingShrink * proportion;

                // Calculate how much this column can actually shrink
                double availableShrink = rightCol.CurrentWidth - rightCol.MinWidth;
                double actualShrink = Math.Min(targetShrink, availableShrink);

                double newRightWidth = rightCol.CurrentWidth - actualShrink;
                newRightWidth = Math.Max(rightCol.MinWidth, newRightWidth);

                double appliedShrink = rightCol.CurrentWidth - newRightWidth;
                shrinkAppliedThisPass += appliedShrink;

                rightCol.CurrentWidth = newRightWidth;
                ApplyWidthToProperty(rightColName, newRightWidth);
            }

            remainingShrink -= shrinkAppliedThisPass;
        }

        // Update scroll state after resize
        UpdateScrollState(visibleColumns);
    }

    /// <summary>
    /// Updates the horizontal scroll state based on current column widths.
    /// </summary>
    private void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth) + 48; // Add padding
        double maxTotalWidth = _availableWidth;

        if (totalWidth > maxTotalWidth + 1)
        {
            // Columns overflow available space - enable horizontal scroll
            _hasManualOverflow = true;
            NeedsHorizontalScroll = true;
            MinimumTotalWidth = totalWidth;
        }
        else
        {
            // Content fits - disable scroll
            // NEVER clear _hasManualOverflow here - only SetAvailableWidth can clear it
            // when the window genuinely gets bigger
            NeedsHorizontalScroll = false;
            MinimumTotalWidth = _columns.Values
                .Where(c => c.IsVisible)
                .Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;
        }
    }

    /// <summary>
    /// Gets the current width of a column.
    /// </summary>
    public double GetColumnWidth(string columnName)
    {
        if (_columns.TryGetValue(columnName, out var col))
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
        if (!_columns.TryGetValue(columnName, out var col)) return;
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
        if (_columns.TryGetValue(columnName, out var col))
        {
            return col.MinWidth;
        }
        return 50;
    }

    /// <summary>
    /// Registers a measured content width for a column (call this for each cell to track max content).
    /// </summary>
    public void RegisterContentWidth(string columnName, double contentWidth)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            // Add padding for cell margins
            var widthWithPadding = contentWidth + 20;
            if (widthWithPadding > col.MeasuredContentWidth)
            {
                col.MeasuredContentWidth = widthWithPadding;
            }
        }
    }

    /// <summary>
    /// Auto-sizes a column to fit its content or preferred width.
    /// Adjusts columns to the right to compensate.
    /// </summary>
    public void AutoSizeColumn(string columnName)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;

        // Determine target width: use measured content if available, otherwise preferred width
        double targetWidth = col.MeasuredContentWidth > 0
            ? Math.Max(col.MinWidth, col.MeasuredContentWidth)
            : col.PreferredWidth;

        targetWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, targetWidth));

        // Calculate delta and use ResizeColumn to properly adjust columns to the right
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
            var visibleColumns = _columns.Values.Where(c => c.IsVisible).ToList();
            if (visibleColumns.Count == 0) return;

            // Calculate minimum total width (sum of all minimums + padding)
            double minTotalWidth = visibleColumns.Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;

            // If user has manually resized causing overflow, check if it still overflows
            if (_hasManualOverflow)
            {
                double totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 48;
                // Use large tolerance - only exit overflow if window is significantly larger
                if (totalWidth + 50 > _availableWidth)
                {
                    // Still overflows - preserve widths
                    MinimumTotalWidth = totalWidth;
                    NeedsHorizontalScroll = true;
                    return;
                }
                else
                {
                    // Window got bigger, columns now fit - clear overflow state
                    _hasManualOverflow = false;
                }
            }

            MinimumTotalWidth = minTotalWidth;

            // Check if we need horizontal scrolling (window too small for minimums)
            NeedsHorizontalScroll = _availableWidth < minTotalWidth;

            // Calculate fixed width total
            double fixedTotal = visibleColumns
                .Where(c => c.IsFixed)
                .Sum(c => c.FixedWidth);

            // Calculate proportional column total star value
            double totalStars = visibleColumns
                .Where(c => !c.IsFixed)
                .Sum(c => c.StarValue);

            // Calculate available width for proportional columns
            // Subtract padding (48px for left/right of 24px each)
            double availableForProportional = _availableWidth - fixedTotal - 48;

            // If we need horizontal scrolling, use minimum widths
            if (NeedsHorizontalScroll)
            {
                foreach (var col in visibleColumns)
                {
                    double width = col.IsFixed ? col.FixedWidth : col.MinWidth;
                    col.CurrentWidth = width;
                    ApplyWidthToProperty(col.Name, width);
                }
                return;
            }

            if (availableForProportional < 0) availableForProportional = 100;

            // Calculate width per star unit
            double widthPerStar = totalStars > 0 ? availableForProportional / totalStars : 0;

            // Apply widths
            foreach (var col in visibleColumns)
            {
                double width;
                if (col.IsFixed)
                {
                    width = col.FixedWidth;
                }
                else
                {
                    width = col.StarValue * widthPerStar;
                    width = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, width));
                }

                col.CurrentWidth = width;
                ApplyWidthToProperty(col.Name, width);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void ApplyWidthToProperty(string columnName, double width)
    {
        switch (columnName)
        {
            case "Id": IdColumnWidth = width; break;
            case "Accountant": AccountantColumnWidth = width; break;
            case "Product": ProductColumnWidth = width; break;
            case "Supplier": SupplierColumnWidth = width; break;
            case "Date": DateColumnWidth = width; break;
            case "Quantity": QuantityColumnWidth = width; break;
            case "UnitPrice": UnitPriceColumnWidth = width; break;
            case "Amount": AmountColumnWidth = width; break;
            case "Tax": TaxColumnWidth = width; break;
            case "Shipping": ShippingColumnWidth = width; break;
            case "Discount": DiscountColumnWidth = width; break;
            case "Total": TotalColumnWidth = width; break;
            case "Receipt": ReceiptColumnWidth = width; break;
            case "Status": StatusColumnWidth = width; break;
            case "Actions": ActionsColumnWidth = width; break;
        }
    }
}
