using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Rental Inventory table.
/// </summary>
public partial class RentalInventoryTableColumnWidths : ObservableObject, ITableColumnWidths
{
    private double _availableWidth = 800;
    private bool _isUpdating;
    private bool _hasManualOverflow;

    private readonly Dictionary<string, ColumnDef> _columns = new();

    private readonly string[] _columnOrder = { "Item", "Supplier", "Status", "TotalQty", "Available", "Rented", "DailyRate", "WeeklyRate", "Deposit", "Actions" };

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

    #region Column Width Properties

    [ObservableProperty]
    private double _itemColumnWidth = 150;

    [ObservableProperty]
    private double _supplierColumnWidth = 120;

    [ObservableProperty]
    private double _statusColumnWidth = 110;

    [ObservableProperty]
    private double _totalQtyColumnWidth = 80;

    [ObservableProperty]
    private double _availableColumnWidth = 80;

    [ObservableProperty]
    private double _rentedColumnWidth = 80;

    [ObservableProperty]
    private double _dailyRateColumnWidth = 90;

    [ObservableProperty]
    private double _weeklyRateColumnWidth = 90;

    [ObservableProperty]
    private double _depositColumnWidth = 90;

    [ObservableProperty]
    private double _actionsColumnWidth = 100;

    [ObservableProperty]
    private double _minimumTotalWidth = 0;

    [ObservableProperty]
    private bool _needsHorizontalScroll = false;

    #endregion

    public RentalInventoryTableColumnWidths()
    {
        InitializeColumns();
    }

    private void InitializeColumns()
    {
        _columns["Item"] = new ColumnDef { Name = "Item", StarValue = 1.4, MinWidth = 120, PreferredWidth = 150 };
        _columns["Supplier"] = new ColumnDef { Name = "Supplier", StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 };
        _columns["Status"] = new ColumnDef { Name = "Status", StarValue = 0.9, MinWidth = 90, PreferredWidth = 110 };
        _columns["TotalQty"] = new ColumnDef { Name = "TotalQty", StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 };
        _columns["Available"] = new ColumnDef { Name = "Available", StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 };
        _columns["Rented"] = new ColumnDef { Name = "Rented", StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 };
        _columns["DailyRate"] = new ColumnDef { Name = "DailyRate", StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 };
        _columns["WeeklyRate"] = new ColumnDef { Name = "WeeklyRate", StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 };
        _columns["Deposit"] = new ColumnDef { Name = "Deposit", StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 };
        _columns["Actions"] = new ColumnDef { Name = "Actions", IsFixed = true, FixedWidth = 100, MinWidth = 100 };
    }

    public void SetColumnVisibility(string columnName, bool isVisible)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            col.IsVisible = isVisible;
            RecalculateWidths();
        }
    }

    public void SetAvailableWidth(double width)
    {
        if (Math.Abs(_availableWidth - width) < 1) return;

        if (_hasManualOverflow)
        {
            var totalWidth = _columns.Values
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

    public void ResizeColumn(string columnName, double delta)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;
        if (Math.Abs(delta) < 0.5) return;

        var visibleColumns = _columnOrder
            .Where(name => _columns.TryGetValue(name, out var c) && c.IsVisible)
            .ToList();

        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return;

        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();

        double totalCurrentWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 48;
        double extraSpace = Math.Max(0, maxTotalWidth - totalCurrentWidth);

        double shrinkableWidth = columnsToRight
            .Where(name => !_columns[name].IsFixed)
            .Sum(name => _columns[name].CurrentWidth - _columns[name].MinWidth);

        var newColWidth = col.CurrentWidth + delta;
        newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, newColWidth));
        var actualDelta = newColWidth - col.CurrentWidth;

        if (Math.Abs(actualDelta) < 0.5) return;

        double projectedTotal = totalCurrentWidth + actualDelta;
        if (projectedTotal > maxTotalWidth)
        {
            _hasManualOverflow = true;
        }

        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        double shrinkNeeded = Math.Max(0, actualDelta - extraSpace);

        if (shrinkNeeded < 0.5)
        {
            UpdateScrollState(visibleColumns);
            return;
        }

        var shrinkableColumns = columnsToRight.Where(name => !_columns[name].IsFixed).ToList();
        if (shrinkableColumns.Count == 0)
        {
            UpdateScrollState(visibleColumns);
            return;
        }

        double remainingShrink = shrinkNeeded;
        const int maxIterations = 10;

        for (int iteration = 0; iteration < maxIterations && remainingShrink > 0.5; iteration++)
        {
            var canShrink = shrinkableColumns
                .Where(name => _columns[name].CurrentWidth > _columns[name].MinWidth + 0.5)
                .ToList();

            if (canShrink.Count == 0) break;

            double shrinkPoolWidth = canShrink.Sum(name => _columns[name].CurrentWidth);
            if (shrinkPoolWidth < 0.5) break;

            double shrinkAppliedThisPass = 0;

            foreach (var rightColName in canShrink)
            {
                var rightCol = _columns[rightColName];
                double proportion = rightCol.CurrentWidth / shrinkPoolWidth;
                double targetShrink = remainingShrink * proportion;

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

        UpdateScrollState(visibleColumns);
    }

    private void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth) + 48;
        double maxTotalWidth = _availableWidth;

        if (totalWidth > maxTotalWidth + 1)
        {
            _hasManualOverflow = true;
            NeedsHorizontalScroll = true;
            MinimumTotalWidth = totalWidth;
        }
        else
        {
            NeedsHorizontalScroll = false;
            MinimumTotalWidth = _columns.Values
                .Where(c => c.IsVisible)
                .Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;
        }
    }

    public double GetColumnWidth(string columnName)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            return col.CurrentWidth > 0 ? col.CurrentWidth : col.MinWidth;
        }
        return 100;
    }

    public void SetColumnWidth(string columnName, double width)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (col.IsFixed) return;

        width = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, width));
        col.CurrentWidth = width;
        ApplyWidthToProperty(columnName, width);
    }

    public double GetMinWidth(string columnName)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            return col.MinWidth;
        }
        return 50;
    }

    public void RegisterContentWidth(string columnName, double contentWidth)
    {
        if (_columns.TryGetValue(columnName, out var col))
        {
            var widthWithPadding = contentWidth + 20;
            if (widthWithPadding > col.MeasuredContentWidth)
            {
                col.MeasuredContentWidth = widthWithPadding;
            }
        }
    }

    public void AutoSizeColumn(string columnName)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
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

    public void RecalculateWidths()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var visibleColumns = _columns.Values.Where(c => c.IsVisible).ToList();
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
                else
                {
                    _hasManualOverflow = false;
                }
            }

            MinimumTotalWidth = minTotalWidth;
            NeedsHorizontalScroll = _availableWidth < minTotalWidth;

            double fixedTotal = visibleColumns
                .Where(c => c.IsFixed)
                .Sum(c => c.FixedWidth);

            double totalStars = visibleColumns
                .Where(c => !c.IsFixed)
                .Sum(c => c.StarValue);

            double availableForProportional = _availableWidth - fixedTotal - 48;

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

            double widthPerStar = totalStars > 0 ? availableForProportional / totalStars : 0;

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
            case "Item": ItemColumnWidth = width; break;
            case "Supplier": SupplierColumnWidth = width; break;
            case "Status": StatusColumnWidth = width; break;
            case "TotalQty": TotalQtyColumnWidth = width; break;
            case "Available": AvailableColumnWidth = width; break;
            case "Rented": RentedColumnWidth = width; break;
            case "DailyRate": DailyRateColumnWidth = width; break;
            case "WeeklyRate": WeeklyRateColumnWidth = width; break;
            case "Deposit": DepositColumnWidth = width; break;
            case "Actions": ActionsColumnWidth = width; break;
        }
    }
}
