using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Stock Adjustments table.
/// Columns: Date | Reference | Product | Location | Type | Quantity | Previous | New | Reason | Actions
/// </summary>
public partial class StockAdjustmentsTableColumnWidths : TableColumnWidthsBase
{
    #region Column Width Properties

    [ObservableProperty]
    private double _dateColumnWidth = 120;

    [ObservableProperty]
    private double _referenceColumnWidth = 100;

    [ObservableProperty]
    private double _productColumnWidth = 160;

    [ObservableProperty]
    private double _locationColumnWidth = 120;

    [ObservableProperty]
    private double _typeColumnWidth = 80;

    [ObservableProperty]
    private double _quantityColumnWidth = 80;

    [ObservableProperty]
    private double _previousColumnWidth = 80;

    [ObservableProperty]
    private double _newColumnWidth = 80;

    [ObservableProperty]
    private double _reasonColumnWidth = 160;

    [ObservableProperty]
    private double _actionsColumnWidth = 100;

    #endregion

    public StockAdjustmentsTableColumnWidths()
    {
        ColumnOrder = ["Date", "Reference", "Product", "Location", "Type", "Quantity", "Previous", "New", "Reason", "Actions"
        ];

        // Date column
        RegisterColumn("Date", new ColumnDef
        {
            StarValue = 0.9,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => DateColumnWidth = w);

        // Reference column
        RegisterColumn("Reference", new ColumnDef
        {
            StarValue = 0.8,
            MinWidth = 80,
            PreferredWidth = 100
        }, w => ReferenceColumnWidth = w);

        // Product column - main identifier
        RegisterColumn("Product", new ColumnDef
        {
            StarValue = 1.3,
            MinWidth = 140,
            PreferredWidth = 160
        }, w => ProductColumnWidth = w);

        // Location column
        RegisterColumn("Location", new ColumnDef
        {
            StarValue = 1.0,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => LocationColumnWidth = w);

        // Type column (narrow)
        RegisterColumn("Type", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => TypeColumnWidth = w);

        // Quantity column (numeric, narrow)
        RegisterColumn("Quantity", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => QuantityColumnWidth = w);

        // Previous column (numeric, narrow)
        RegisterColumn("Previous", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => PreviousColumnWidth = w);

        // New column (numeric, narrow)
        RegisterColumn("New", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => NewColumnWidth = w);

        // Reason column
        RegisterColumn("Reason", new ColumnDef
        {
            StarValue = 1.3,
            MinWidth = 140,
            PreferredWidth = 160
        }, w => ReasonColumnWidth = w);

        // Actions column (fixed width)
        RegisterColumn("Actions", new ColumnDef
        {
            IsFixed = true,
            FixedWidth = 100,
            MinWidth = 100
        }, w => ActionsColumnWidth = w);

        // Initial calculation
        RecalculateWidths();
    }
}
