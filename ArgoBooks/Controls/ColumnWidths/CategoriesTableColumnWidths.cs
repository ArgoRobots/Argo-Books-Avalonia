using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Categories table.
/// </summary>
public partial class CategoriesTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _nameColumnWidth = 200;

    [ObservableProperty]
    private double _parentColumnWidth = 140;

    [ObservableProperty]
    private double _descriptionColumnWidth = 200;

    [ObservableProperty]
    private double _typeColumnWidth = 100;

    [ObservableProperty]
    private double _productCountColumnWidth = 120;

    [ObservableProperty]
    private double _actionsColumnWidth = 140;

    public CategoriesTableColumnWidths()
    {
        ColumnOrder = new[] { "Name", "Parent", "Description", "Type", "ProductCount", "Actions" };

        RegisterColumn("Name", new ColumnDef { StarValue = 1.5, MinWidth = 150, PreferredWidth = 200 }, w => NameColumnWidth = w);
        RegisterColumn("Parent", new ColumnDef { StarValue = 1.0, MinWidth = 100, PreferredWidth = 140 }, w => ParentColumnWidth = w);
        RegisterColumn("Description", new ColumnDef { StarValue = 1.5, MinWidth = 150, PreferredWidth = 200 }, w => DescriptionColumnWidth = w);
        RegisterColumn("Type", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 100 }, w => TypeColumnWidth = w);
        RegisterColumn("ProductCount", new ColumnDef { StarValue = 0.8, MinWidth = 80, PreferredWidth = 120 }, w => ProductCountColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = 140, MinWidth = 140 }, w => ActionsColumnWidth = w);
    }
}
