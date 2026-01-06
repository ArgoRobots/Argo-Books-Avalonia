using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Manages column widths for the Stock Levels table.
/// Columns: Product | SKU | Category | Location | In Stock | Reserved | Available | Reorder Point | Status | Actions
/// </summary>
public partial class StockLevelsTableColumnWidths : TableColumnWidthsBase
{
    #region Column Width Properties

    [ObservableProperty]
    private double _productColumnWidth = 180;

    [ObservableProperty]
    private double _skuColumnWidth = 100;

    [ObservableProperty]
    private double _categoryColumnWidth = 120;

    [ObservableProperty]
    private double _locationColumnWidth = 120;

    [ObservableProperty]
    private double _inStockColumnWidth = 80;

    [ObservableProperty]
    private double _reservedColumnWidth = 80;

    [ObservableProperty]
    private double _availableColumnWidth = 80;

    [ObservableProperty]
    private double _reorderPointColumnWidth = 100;

    [ObservableProperty]
    private double _statusColumnWidth = 100;

    [ObservableProperty]
    private double _actionsColumnWidth = 140;

    #endregion

    public StockLevelsTableColumnWidths()
    {
        ColumnOrder = new[] { "Product", "Sku", "Category", "Location", "InStock", "Reserved", "Available", "ReorderPoint", "Status", "Actions" };

        // Product column - main identifier
        RegisterColumn("Product", new ColumnDef
        {
            StarValue = 1.5,
            MinWidth = 150,
            PreferredWidth = 180
        }, w => ProductColumnWidth = w);

        // SKU column
        RegisterColumn("Sku", new ColumnDef
        {
            StarValue = 0.8,
            MinWidth = 80,
            PreferredWidth = 100
        }, w => SkuColumnWidth = w);

        // Category column
        RegisterColumn("Category", new ColumnDef
        {
            StarValue = 1.0,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => CategoryColumnWidth = w);

        // Location column
        RegisterColumn("Location", new ColumnDef
        {
            StarValue = 1.0,
            MinWidth = 100,
            PreferredWidth = 120
        }, w => LocationColumnWidth = w);

        // In Stock column (numeric, narrower)
        RegisterColumn("InStock", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => InStockColumnWidth = w);

        // Reserved column (numeric, narrower)
        RegisterColumn("Reserved", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => ReservedColumnWidth = w);

        // Available column (numeric, narrower)
        RegisterColumn("Available", new ColumnDef
        {
            StarValue = 0.6,
            MinWidth = 70,
            PreferredWidth = 80
        }, w => AvailableColumnWidth = w);

        // Reorder Point column
        RegisterColumn("ReorderPoint", new ColumnDef
        {
            StarValue = 0.8,
            MinWidth = 80,
            PreferredWidth = 100
        }, w => ReorderPointColumnWidth = w);

        // Status column
        RegisterColumn("Status", new ColumnDef
        {
            StarValue = 0.8,
            MinWidth = 80,
            PreferredWidth = 100
        }, w => StatusColumnWidth = w);

        // Actions column (fixed width)
        RegisterColumn("Actions", new ColumnDef
        {
            IsFixed = true,
            FixedWidth = 140,
            MinWidth = 140
        }, w => ActionsColumnWidth = w);

        // Initial calculation
        RecalculateWidths();
    }
}
