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

    // Column definitions with star values (proportion weights)
    private readonly Dictionary<string, ColumnDef> _columns = new();

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

    #endregion

    public TableColumnWidths()
    {
        InitializeColumns();
    }

    private void InitializeColumns()
    {
        _columns["Id"] = new ColumnDef { Name = "Id", StarValue = 1.2, MinWidth = 80 };
        _columns["Accountant"] = new ColumnDef { Name = "Accountant", StarValue = 1.0, MinWidth = 70 };
        _columns["Product"] = new ColumnDef { Name = "Product", StarValue = 1.5, MinWidth = 100 };
        _columns["Supplier"] = new ColumnDef { Name = "Supplier", StarValue = 1.2, MinWidth = 80 };
        _columns["Date"] = new ColumnDef { Name = "Date", StarValue = 1.0, MinWidth = 80 };
        _columns["Quantity"] = new ColumnDef { Name = "Quantity", StarValue = 0.5, MinWidth = 40 };
        _columns["UnitPrice"] = new ColumnDef { Name = "UnitPrice", StarValue = 0.8, MinWidth = 60 };
        _columns["Amount"] = new ColumnDef { Name = "Amount", StarValue = 0.8, MinWidth = 60 };
        _columns["Tax"] = new ColumnDef { Name = "Tax", StarValue = 0.6, MinWidth = 50 };
        _columns["Shipping"] = new ColumnDef { Name = "Shipping", StarValue = 0.7, MinWidth = 60 };
        _columns["Discount"] = new ColumnDef { Name = "Discount", StarValue = 0.7, MinWidth = 60 };
        _columns["Total"] = new ColumnDef { Name = "Total", StarValue = 0.8, MinWidth = 70 };
        _columns["Receipt"] = new ColumnDef { Name = "Receipt", StarValue = 0.5, MinWidth = 50 };
        _columns["Status"] = new ColumnDef { Name = "Status", StarValue = 0.9, MinWidth = 80 };
        _columns["Actions"] = new ColumnDef { Name = "Actions", IsFixed = true, FixedWidth = 160, MinWidth = 120 };
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
        _availableWidth = width;
        RecalculateWidths();
    }

    /// <summary>
    /// Adjusts a column width by a delta amount (from resize drag).
    /// </summary>
    public void ResizeColumn(string columnName, double delta)
    {
        if (!_columns.TryGetValue(columnName, out var col)) return;
        if (!col.IsVisible || col.IsFixed) return;

        var newWidth = col.CurrentWidth + delta;
        newWidth = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, newWidth));
        col.CurrentWidth = newWidth;

        ApplyWidthToProperty(columnName, newWidth);
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
