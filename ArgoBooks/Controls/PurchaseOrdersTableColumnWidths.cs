using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Purchase Orders table.
/// Columns: PO Number | Date | Supplier | Items | Total | Status | Expected | Actions
/// </summary>
public partial class PurchaseOrdersTableColumnWidths : TableColumnWidthsBase
{
    #region Column Width Properties

    [ObservableProperty]
    private double _poNumberColumnWidth = 120;

    [ObservableProperty]
    private double _dateColumnWidth = 110;

    [ObservableProperty]
    private double _supplierColumnWidth = 180;

    [ObservableProperty]
    private double _itemsColumnWidth = 80;

    [ObservableProperty]
    private double _totalColumnWidth = 110;

    [ObservableProperty]
    private double _statusColumnWidth = 120;

    [ObservableProperty]
    private double _expectedColumnWidth = 110;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    #endregion

    public PurchaseOrdersTableColumnWidths()
    {
        ColumnOrder = new[] { "PONumber", "Date", "Supplier", "Items", "Total", "Status", "Expected", "Actions" };

        // PO Number column
        RegisterColumn("PONumber", new ColumnDef
        {
            StarValue = 1.0,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => PoNumberColumnWidth = w);

        // Date column
        RegisterColumn("Date", new ColumnDef
        {
            StarValue = 0.9,
            MinWidth = 90,
            PreferredWidth = 110
        }, w => DateColumnWidth = w);

        // Supplier column - main identifier
        RegisterColumn("Supplier", new ColumnDef
        {
            StarValue = 1.5,
            MinWidth = 150,
            PreferredWidth = 180
        }, w => SupplierColumnWidth = w);

        // Items column (numeric, narrow)
        RegisterColumn("Items", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 60,
            PreferredWidth = 80
        }, w => ItemsColumnWidth = w);

        // Total column (currency)
        RegisterColumn("Total", new ColumnDef
        {
            StarValue = 0.9,
            MinWidth = 90,
            PreferredWidth = 110
        }, w => TotalColumnWidth = w);

        // Status column
        RegisterColumn("Status", new ColumnDef
        {
            StarValue = 1.0,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => StatusColumnWidth = w);

        // Expected column
        RegisterColumn("Expected", new ColumnDef
        {
            StarValue = 0.9,
            MinWidth = 90,
            PreferredWidth = 110
        }, w => ExpectedColumnWidth = w);

        // Actions column (fixed width)
        RegisterColumn("Actions", new ColumnDef
        {
            IsFixed = true,
            FixedWidth = 120,
            MinWidth = 120
        }, w => ActionsColumnWidth = w);

        // Initial calculation
        RecalculateWidths();
    }
}
