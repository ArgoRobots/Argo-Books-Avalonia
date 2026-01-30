using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Revenue table.
/// </summary>
public partial class RevenueTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _idColumnWidth = 80;

    [ObservableProperty]
    private double _accountantColumnWidth = 120;

    [ObservableProperty]
    private double _customerColumnWidth = 150;

    [ObservableProperty]
    private double _productColumnWidth = 180;

    [ObservableProperty]
    private double _dateColumnWidth = 100;

    [ObservableProperty]
    private double _quantityColumnWidth = 60;

    [ObservableProperty]
    private double _unitPriceColumnWidth = 90;

    [ObservableProperty]
    private double _amountColumnWidth = 90;

    [ObservableProperty]
    private double _taxColumnWidth = 70;

    [ObservableProperty]
    private double _shippingColumnWidth = 80;

    [ObservableProperty]
    private double _discountColumnWidth = 80;

    [ObservableProperty]
    private double _totalColumnWidth = 90;

    [ObservableProperty]
    private double _receiptColumnWidth = 70;

    [ObservableProperty]
    private double _statusColumnWidth = 90;

    [ObservableProperty]
    private double _actionsColumnWidth = 156;

    public RevenueTableColumnWidths()
    {
        ColumnOrder = ["Id", "Accountant", "Customer", "Product", "Date", "Quantity", "UnitPrice", "Amount", "Tax", "Shipping", "Discount", "Total", "Receipt", "Status", "Actions"
        ];

        RegisterColumn("Id", new ColumnDef { StarValue = 0.8, MinWidth = 60, PreferredWidth = 80 }, w => IdColumnWidth = w);
        RegisterColumn("Accountant", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 }, w => AccountantColumnWidth = w);
        RegisterColumn("Customer", new ColumnDef { StarValue = 1.2, MinWidth = 100, PreferredWidth = 150 }, w => CustomerColumnWidth = w);
        RegisterColumn("Product", new ColumnDef { StarValue = 1.5, MinWidth = 120, PreferredWidth = 180 }, w => ProductColumnWidth = w);
        RegisterColumn("Date", new ColumnDef { StarValue = 0.9, MinWidth = 80, PreferredWidth = 100 }, w => DateColumnWidth = w);
        RegisterColumn("Quantity", new ColumnDef { StarValue = 0.5, MinWidth = 40, PreferredWidth = 60 }, w => QuantityColumnWidth = w);
        RegisterColumn("UnitPrice", new ColumnDef { StarValue = 0.8, MinWidth = 70, PreferredWidth = 90 }, w => UnitPriceColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.8, MinWidth = 70, PreferredWidth = 90 }, w => AmountColumnWidth = w);
        RegisterColumn("Tax", new ColumnDef { StarValue = 0.7, MinWidth = 50, PreferredWidth = 70 }, w => TaxColumnWidth = w);
        RegisterColumn("Shipping", new ColumnDef { StarValue = 0.7, MinWidth = 60, PreferredWidth = 80 }, w => ShippingColumnWidth = w);
        RegisterColumn("Discount", new ColumnDef { StarValue = 0.7, MinWidth = 60, PreferredWidth = 80 }, w => DiscountColumnWidth = w);
        RegisterColumn("Total", new ColumnDef { StarValue = 0.8, MinWidth = 70, PreferredWidth = 90 }, w => TotalColumnWidth = w);
        RegisterColumn("Receipt", new ColumnDef { StarValue = 0.8, MinWidth = 50, PreferredWidth = 70 }, w => ReceiptColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.6, MinWidth = 70, PreferredWidth = 90 }, w => StatusColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = ActionsWidth(4), MinWidth = ActionsWidth(4) }, w => ActionsColumnWidth = w);
    }
}
