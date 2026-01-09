using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Products table.
/// Supports both Expenses tab (9 columns) and Revenue tab (5 columns).
/// </summary>
public partial class ProductsTableColumnWidths : ObservableObject, ITableColumnWidths
{
    private double _availableWidth = 800;
    private bool _isUpdating;
    private bool _hasManualOverflow;
    private bool _isExpensesTab = true;

    private readonly Dictionary<string, ColumnDef> _columns = new();

    // Column order for Expenses tab
    private readonly string[] _expensesColumnOrder = ["Name", "Type", "Description", "Category", "Supplier", "Country", "Reorder", "Overstock", "Actions"
    ];

    // Column order for Revenue tab
    private readonly string[] _revenueColumnOrder = ["Name", "Type", "Description", "Category", "Actions"];

    public class ColumnDef
    {
        public string Name { get; set; } = "";
        public double ExpensesStarValue { get; set; } = 1.0;
        public double RevenueStarValue { get; set; } = 1.0;
        public double MinWidth { get; set; } = 50;
        public double MaxWidth { get; set; } = double.PositiveInfinity;
        public bool IsVisibleInExpenses { get; set; } = true;
        public bool IsVisibleInRevenue { get; set; } = true;
        public bool IsFixed { get; set; }
        public double FixedWidth { get; set; } = 100;
        public double CurrentWidth { get; set; }
        public double PreferredWidth { get; set; } = 120;
        public double MeasuredContentWidth { get; set; }
    }

    #region Column Width Properties

    [ObservableProperty]
    private double _nameColumnWidth = 150;

    [ObservableProperty]
    private double _typeColumnWidth = 80;

    [ObservableProperty]
    private double _descriptionColumnWidth = 150;

    [ObservableProperty]
    private double _categoryColumnWidth = 100;

    [ObservableProperty]
    private double _supplierColumnWidth = 100;

    [ObservableProperty]
    private double _countryColumnWidth = 100;

    [ObservableProperty]
    private double _reorderColumnWidth = 80;

    [ObservableProperty]
    private double _overstockColumnWidth = 80;

    [ObservableProperty]
    private double _actionsColumnWidth = 100;

    [ObservableProperty]
    private double _minimumTotalWidth = 0;

    [ObservableProperty]
    private bool _needsHorizontalScroll = false;

    #endregion

    public ProductsTableColumnWidths()
    {
        InitializeColumns();
    }

    private void InitializeColumns()
    {
        // Name column
        _columns["Name"] = new ColumnDef
        {
            Name = "Name",
            ExpensesStarValue = 1.2,
            RevenueStarValue = 1.5,
            MinWidth = 120,
            PreferredWidth = 150,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = true
        };

        // Type column
        _columns["Type"] = new ColumnDef
        {
            Name = "Type",
            ExpensesStarValue = 0.6,
            RevenueStarValue = 0.8,
            MinWidth = 60,
            PreferredWidth = 80,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = true
        };

        // Description column
        _columns["Description"] = new ColumnDef
        {
            Name = "Description",
            ExpensesStarValue = 1.2,
            RevenueStarValue = 2.0,
            MinWidth = 100,
            PreferredWidth = 150,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = true
        };

        // Category column
        _columns["Category"] = new ColumnDef
        {
            Name = "Category",
            ExpensesStarValue = 0.8,
            RevenueStarValue = 1.0,
            MinWidth = 80,
            PreferredWidth = 100,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = true
        };

        // Supplier column (Expenses only)
        _columns["Supplier"] = new ColumnDef
        {
            Name = "Supplier",
            ExpensesStarValue = 0.8,
            RevenueStarValue = 0,
            MinWidth = 80,
            PreferredWidth = 100,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = false
        };

        // Country column (Expenses only)
        _columns["Country"] = new ColumnDef
        {
            Name = "Country",
            ExpensesStarValue = 0.8,
            RevenueStarValue = 0,
            MinWidth = 80,
            PreferredWidth = 100,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = false
        };

        // Reorder column (Expenses only)
        _columns["Reorder"] = new ColumnDef
        {
            Name = "Reorder",
            ExpensesStarValue = 0.6,
            RevenueStarValue = 0,
            MinWidth = 60,
            PreferredWidth = 80,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = false
        };

        // Overstock column (Expenses only)
        _columns["Overstock"] = new ColumnDef
        {
            Name = "Overstock",
            ExpensesStarValue = 0.6,
            RevenueStarValue = 0,
            MinWidth = 60,
            PreferredWidth = 80,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = false
        };

        // Actions column (fixed width)
        _columns["Actions"] = new ColumnDef
        {
            Name = "Actions",
            IsFixed = true,
            FixedWidth = 100,
            MinWidth = 100,
            IsVisibleInExpenses = true,
            IsVisibleInRevenue = true
        };
    }

    public void SetTabMode(bool isExpensesTab)
    {
        if (_isExpensesTab == isExpensesTab) return;
        _isExpensesTab = isExpensesTab;
        _hasManualOverflow = false;
        RecalculateWidths();
    }

    private string[] GetCurrentColumnOrder()
    {
        return _isExpensesTab ? _expensesColumnOrder : _revenueColumnOrder;
    }

    private bool IsColumnVisible(ColumnDef col)
    {
        return _isExpensesTab ? col.IsVisibleInExpenses : col.IsVisibleInRevenue;
    }

    private double GetColumnStarValue(ColumnDef col)
    {
        return _isExpensesTab ? col.ExpensesStarValue : col.RevenueStarValue;
    }

    public void SetAvailableWidth(double width)
    {
        if (Math.Abs(_availableWidth - width) < 1) return;

        if (_hasManualOverflow)
        {
            var visibleColumns = _columns.Values.Where(IsColumnVisible).ToList();
            var totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 48;
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
        if (!IsColumnVisible(col) || col.IsFixed) return;
        if (Math.Abs(delta) < 0.5) return;

        var columnOrder = GetCurrentColumnOrder();
        var visibleColumns = columnOrder.Where(name => _columns.TryGetValue(name, out var c) && IsColumnVisible(c)).ToList();
        var columnIndex = visibleColumns.IndexOf(columnName);
        if (columnIndex < 0) return;

        var columnsToRight = visibleColumns.Skip(columnIndex + 1).ToList();
        double totalCurrentWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth);
        double maxTotalWidth = _availableWidth - 48;

        var newColWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, col.CurrentWidth + delta));
        var actualDelta = newColWidth - col.CurrentWidth;
        if (Math.Abs(actualDelta) < 0.5) return;

        if (totalCurrentWidth + actualDelta > maxTotalWidth) _hasManualOverflow = true;

        col.CurrentWidth = newColWidth;
        ApplyWidthToProperty(columnName, newColWidth);

        double shrinkNeeded = Math.Max(0, actualDelta - Math.Max(0, maxTotalWidth - totalCurrentWidth));
        if (shrinkNeeded > 0.5)
        {
            var shrinkableColumns = columnsToRight.Where(name => !_columns[name].IsFixed).ToList();
            foreach (var rightColName in shrinkableColumns)
            {
                var rightCol = _columns[rightColName];
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

    private void UpdateScrollState(List<string> visibleColumns)
    {
        double totalWidth = visibleColumns.Sum(name => _columns[name].CurrentWidth) + 48;
        if (totalWidth > _availableWidth + 1)
        {
            _hasManualOverflow = true;
            NeedsHorizontalScroll = true;
            MinimumTotalWidth = totalWidth;
        }
        else
        {
            NeedsHorizontalScroll = false;
            MinimumTotalWidth = _columns.Values.Where(IsColumnVisible).Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;
        }
    }

    public void AutoSizeColumn(string columnName)
    {
        if (!_columns.TryGetValue(columnName, out var col) || !IsColumnVisible(col) || col.IsFixed) return;
        double targetWidth = Math.Max(col.MinWidth, col.MeasuredContentWidth > 0 ? col.MeasuredContentWidth : col.PreferredWidth);
        var delta = targetWidth - col.CurrentWidth;
        if (Math.Abs(delta) > 0.5) ResizeColumn(columnName, delta);
    }

    public void RecalculateWidths()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            var visibleColumns = _columns.Values.Where(IsColumnVisible).ToList();
            if (visibleColumns.Count == 0) return;

            double minTotalWidth = visibleColumns.Sum(c => c.IsFixed ? c.FixedWidth : c.MinWidth) + 48;

            if (_hasManualOverflow)
            {
                double totalWidth = visibleColumns.Sum(c => c.CurrentWidth) + 48;
                if (totalWidth + 50 > _availableWidth) { MinimumTotalWidth = totalWidth; NeedsHorizontalScroll = true; return; }
                _hasManualOverflow = false;
            }

            MinimumTotalWidth = minTotalWidth;
            NeedsHorizontalScroll = _availableWidth < minTotalWidth;

            double fixedTotal = visibleColumns.Where(c => c.IsFixed).Sum(c => c.FixedWidth);
            double totalStars = visibleColumns.Where(c => !c.IsFixed).Sum(c => GetColumnStarValue(c));
            double availableForProportional = Math.Max(100, _availableWidth - fixedTotal - 48);

            if (NeedsHorizontalScroll)
            {
                foreach (var col in visibleColumns) { col.CurrentWidth = col.IsFixed ? col.FixedWidth : col.MinWidth; ApplyWidthToProperty(col.Name, col.CurrentWidth); }
                return;
            }

            double widthPerStar = totalStars > 0 ? availableForProportional / totalStars : 0;
            foreach (var col in visibleColumns)
            {
                col.CurrentWidth = col.IsFixed ? col.FixedWidth : Math.Max(col.MinWidth, Math.Min(col.MaxWidth, GetColumnStarValue(col) * widthPerStar));
                ApplyWidthToProperty(col.Name, col.CurrentWidth);
            }
        }
        finally { _isUpdating = false; }
    }

    private void ApplyWidthToProperty(string columnName, double width)
    {
        switch (columnName)
        {
            case "Name": NameColumnWidth = width; break;
            case "Type": TypeColumnWidth = width; break;
            case "Description": DescriptionColumnWidth = width; break;
            case "Category": CategoryColumnWidth = width; break;
            case "Supplier": SupplierColumnWidth = width; break;
            case "Country": CountryColumnWidth = width; break;
            case "Reorder": ReorderColumnWidth = width; break;
            case "Overstock": OverstockColumnWidth = width; break;
            case "Actions": ActionsColumnWidth = width; break;
        }
    }
}
