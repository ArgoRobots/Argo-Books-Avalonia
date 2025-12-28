using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Receipts table.
/// </summary>
public partial class ReceiptsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _idColumnWidth = 120;

    [ObservableProperty]
    private double _vendorColumnWidth = 180;

    [ObservableProperty]
    private double _dateColumnWidth = 120;

    [ObservableProperty]
    private double _typeColumnWidth = 100;

    [ObservableProperty]
    private double _amountColumnWidth = 100;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public ReceiptsTableColumnWidths()
    {
        ColumnOrder = new[] { "Id", "Vendor", "Date", "Type", "Amount", "Actions" };

        RegisterColumn("Id", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 }, w => IdColumnWidth = w);
        RegisterColumn("Vendor", new ColumnDef { StarValue = 1.5, MinWidth = 140, PreferredWidth = 180 }, w => VendorColumnWidth = w);
        RegisterColumn("Date", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 120 }, w => DateColumnWidth = w);
        RegisterColumn("Type", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => TypeColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => AmountColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 120, MinWidth = 120 }, w => ActionsColumnWidth = w);
    }
}
