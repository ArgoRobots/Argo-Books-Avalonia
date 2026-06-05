using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Bank Matching "Missing from statement" table.
/// Columns: Date | Description | Amount
/// </summary>
public partial class MissingRecordsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _dateColumnWidth = 130;

    [ObservableProperty]
    private double _descriptionColumnWidth = 320;

    [ObservableProperty]
    private double _amountColumnWidth = 130;

    public MissingRecordsTableColumnWidths()
    {
        ColumnOrder = ["Date", "Description", "Amount"];

        RegisterColumn("Date", new ColumnDef { StarValue = 0.9, MinWidth = 100, PreferredWidth = 130 }, w => DateColumnWidth = w);
        RegisterColumn("Description", new ColumnDef { StarValue = 2.8, MinWidth = 200, PreferredWidth = 320 }, w => DescriptionColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.9, MinWidth = 100, PreferredWidth = 130 }, w => AmountColumnWidth = w);

        RecalculateWidths();
    }
}
