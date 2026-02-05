using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Payments table.
/// </summary>
public partial class PaymentsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _idColumnWidth = 100;

    [ObservableProperty]
    private double _invoiceColumnWidth = 100;

    [ObservableProperty]
    private double _customerColumnWidth = 160;

    [ObservableProperty]
    private double _dateColumnWidth = 110;

    [ObservableProperty]
    private double _methodColumnWidth = 90;

    [ObservableProperty]
    private double _amountColumnWidth = 100;

    [ObservableProperty]
    private double _statusColumnWidth = 100;

    [ObservableProperty]
    private double _actionsColumnWidth = 84;

    public PaymentsTableColumnWidths()
    {
        ColumnOrder = ["Id", "Invoice", "Customer", "Date", "Method", "Amount", "Status", "Actions"];

        RegisterColumn("Id", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => IdColumnWidth = w);
        RegisterColumn("Invoice", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => InvoiceColumnWidth = w);
        RegisterColumn("Customer", new ColumnDef { StarValue = 1.2, MinWidth = 120, PreferredWidth = 160 }, w => CustomerColumnWidth = w);
        RegisterColumn("Date", new ColumnDef { StarValue = 0.8, MinWidth = 90, PreferredWidth = 110 }, w => DateColumnWidth = w);
        RegisterColumn("Method", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 }, w => MethodColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => AmountColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.7, MinWidth = 80, PreferredWidth = 100 }, w => StatusColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = ActionsWidth(2), MinWidth = ActionsWidth(2) }, w => ActionsColumnWidth = w);

        InitializeColumnWidths();
    }
}
