using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Rental Records table.
/// </summary>
public partial class RentalRecordsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _idColumnWidth = 100;

    [ObservableProperty]
    private double _accountantColumnWidth = 120;

    [ObservableProperty]
    private double _itemColumnWidth = 180;

    [ObservableProperty]
    private double _customerColumnWidth = 180;

    [ObservableProperty]
    private double _quantityColumnWidth = 80;

    [ObservableProperty]
    private double _startDateColumnWidth = 110;

    [ObservableProperty]
    private double _dueDateColumnWidth = 110;

    [ObservableProperty]
    private double _statusColumnWidth = 110;

    [ObservableProperty]
    private double _totalColumnWidth = 100;

    [ObservableProperty]
    private double _depositColumnWidth = 100;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public RentalRecordsTableColumnWidths()
    {
        ColumnOrder = new[] { "Id", "Accountant", "Item", "Customer", "Quantity", "StartDate", "DueDate", "Status", "Total", "Deposit", "Actions" };

        RegisterColumn("Id", new ColumnDef { StarValue = 0.6, MinWidth = 80, PreferredWidth = 100 }, w => IdColumnWidth = w);
        RegisterColumn("Accountant", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 }, w => AccountantColumnWidth = w);
        RegisterColumn("Item", new ColumnDef { StarValue = 1.0, MinWidth = 140, PreferredWidth = 180 }, w => ItemColumnWidth = w);
        RegisterColumn("Customer", new ColumnDef { StarValue = 1.0, MinWidth = 140, PreferredWidth = 180 }, w => CustomerColumnWidth = w);
        RegisterColumn("Quantity", new ColumnDef { StarValue = 0.4, MinWidth = 60, PreferredWidth = 80 }, w => QuantityColumnWidth = w);
        RegisterColumn("StartDate", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => StartDateColumnWidth = w);
        RegisterColumn("DueDate", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => DueDateColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => StatusColumnWidth = w);
        RegisterColumn("Total", new ColumnDef { StarValue = 0.6, MinWidth = 80, PreferredWidth = 100 }, w => TotalColumnWidth = w);
        RegisterColumn("Deposit", new ColumnDef { StarValue = 0.6, MinWidth = 80, PreferredWidth = 100 }, w => DepositColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 120, MinWidth = 120 }, w => ActionsColumnWidth = w);
    }
}
