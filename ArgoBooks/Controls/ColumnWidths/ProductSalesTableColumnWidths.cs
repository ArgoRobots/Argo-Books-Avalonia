using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Analytics "Sales by Product" table.
/// Columns: Product | Units | Revenue | Avg price
/// </summary>
public partial class ProductSalesTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _productColumnWidth = 280;

    [ObservableProperty]
    private double _unitsColumnWidth = 110;

    [ObservableProperty]
    private double _revenueColumnWidth = 150;

    [ObservableProperty]
    private double _avgPriceColumnWidth = 150;

    public ProductSalesTableColumnWidths()
    {
        ColumnOrder = ["Product", "Units", "Revenue", "AvgPrice"];

        RegisterColumn("Product", new ColumnDef { StarValue = 2.6, MinWidth = 160, PreferredWidth = 280 }, w => ProductColumnWidth = w);
        RegisterColumn("Units", new ColumnDef { StarValue = 1.0, MinWidth = 80, PreferredWidth = 110 }, w => UnitsColumnWidth = w);
        RegisterColumn("Revenue", new ColumnDef { StarValue = 1.3, MinWidth = 110, PreferredWidth = 150 }, w => RevenueColumnWidth = w);
        RegisterColumn("AvgPrice", new ColumnDef { StarValue = 1.3, MinWidth = 110, PreferredWidth = 150 }, w => AvgPriceColumnWidth = w);

        RecalculateWidths();
    }
}
