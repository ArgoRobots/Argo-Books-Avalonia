using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Rental Inventory table.
/// </summary>
public partial class RentalInventoryTableColumnWidths : TableColumnWidthsBase
{
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
    private double _actionsColumnWidth = 120;

    public RentalInventoryTableColumnWidths()
    {
        ColumnOrder = ["Item", "Supplier", "Status", "TotalQty", "Available", "Rented", "DailyRate", "WeeklyRate", "Deposit", "Actions"
        ];

        RegisterColumn("Item", new ColumnDef { StarValue = 1.4, MinWidth = 120, PreferredWidth = 150 }, w => ItemColumnWidth = w);
        RegisterColumn("Supplier", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 }, w => SupplierColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.9, MinWidth = 90, PreferredWidth = 110 }, w => StatusColumnWidth = w);
        RegisterColumn("TotalQty", new ColumnDef { StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 }, w => TotalQtyColumnWidth = w);
        RegisterColumn("Available", new ColumnDef { StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 }, w => AvailableColumnWidth = w);
        RegisterColumn("Rented", new ColumnDef { StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 }, w => RentedColumnWidth = w);
        RegisterColumn("DailyRate", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 }, w => DailyRateColumnWidth = w);
        RegisterColumn("WeeklyRate", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 }, w => WeeklyRateColumnWidth = w);
        RegisterColumn("Deposit", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 }, w => DepositColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = ActionsWidth(3), MinWidth = ActionsWidth(3) }, w => ActionsColumnWidth = w);
    }
}
