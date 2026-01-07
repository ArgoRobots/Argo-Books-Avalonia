using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Suppliers table.
/// </summary>
public partial class SuppliersTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _supplierColumnWidth = 200;

    [ObservableProperty]
    private double _contactColumnWidth = 150;

    [ObservableProperty]
    private double _countryColumnWidth = 130;

    [ObservableProperty]
    private double _productsColumnWidth = 100;

    [ObservableProperty]
    private double _statusColumnWidth = 90;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public SuppliersTableColumnWidths()
    {
        ColumnOrder = new[] { "Supplier", "Contact", "Country", "Products", "Status", "Actions" };

        RegisterColumn("Supplier", new ColumnDef { StarValue = 1.5, MinWidth = 150, PreferredWidth = 200 }, w => SupplierColumnWidth = w);
        RegisterColumn("Contact", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 150 }, w => ContactColumnWidth = w);
        RegisterColumn("Country", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 130 }, w => CountryColumnWidth = w);
        RegisterColumn("Products", new ColumnDef { StarValue = 0.8, MinWidth = 60, PreferredWidth = 100 }, w => ProductsColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.6, MinWidth = 70, PreferredWidth = 90 }, w => StatusColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 120, MinWidth = 120 }, w => ActionsColumnWidth = w);
    }
}
