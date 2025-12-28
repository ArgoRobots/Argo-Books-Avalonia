using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Lost/Damaged table.
/// </summary>
public partial class LostDamagedTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _idColumnWidth = 90;

    [ObservableProperty]
    private double _typeColumnWidth = 70;

    [ObservableProperty]
    private double _productColumnWidth = 150;

    [ObservableProperty]
    private double _dateColumnWidth = 85;

    [ObservableProperty]
    private double _reasonColumnWidth = 100;

    [ObservableProperty]
    private double _staffColumnWidth = 100;

    [ObservableProperty]
    private double _lossColumnWidth = 80;

    [ObservableProperty]
    private double _statusColumnWidth = 85;

    [ObservableProperty]
    private double _actionsColumnWidth = 70;

    public LostDamagedTableColumnWidths()
    {
        ColumnOrder = new[] { "Id", "Type", "Product", "Date", "Reason", "Staff", "Loss", "Status", "Actions" };

        RegisterColumn("Id", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 90 }, w => IdColumnWidth = w);
        RegisterColumn("Type", new ColumnDef { StarValue = 0.6, MinWidth = 60, PreferredWidth = 70 }, w => TypeColumnWidth = w);
        RegisterColumn("Product", new ColumnDef { StarValue = 1.2, MinWidth = 100, PreferredWidth = 150 }, w => ProductColumnWidth = w);
        RegisterColumn("Date", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 85 }, w => DateColumnWidth = w);
        RegisterColumn("Reason", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => ReasonColumnWidth = w);
        RegisterColumn("Staff", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => StaffColumnWidth = w);
        RegisterColumn("Loss", new ColumnDef { StarValue = 0.6, MinWidth = 60, PreferredWidth = 80 }, w => LossColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.7, MinWidth = 70, PreferredWidth = 85 }, w => StatusColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 70, MinWidth = 70 }, w => ActionsColumnWidth = w);
    }
}
