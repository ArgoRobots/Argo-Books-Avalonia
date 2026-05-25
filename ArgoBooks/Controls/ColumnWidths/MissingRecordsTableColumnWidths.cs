using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Bank Matching "Missing from statement" table.
/// Columns: Date | Description | Amount | Note
/// </summary>
public partial class MissingRecordsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _dateColumnWidth = 120;

    [ObservableProperty]
    private double _descriptionColumnWidth = 240;

    [ObservableProperty]
    private double _amountColumnWidth = 120;

    [ObservableProperty]
    private double _noteColumnWidth = 150;

    public MissingRecordsTableColumnWidths()
    {
        ColumnOrder = ["Date", "Description", "Amount", "Note"];

        RegisterColumn("Date", new ColumnDef { StarValue = 0.9, MinWidth = 100, PreferredWidth = 120 }, w => DateColumnWidth = w);
        RegisterColumn("Description", new ColumnDef { StarValue = 2.2, MinWidth = 180, PreferredWidth = 240 }, w => DescriptionColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.9, MinWidth = 100, PreferredWidth = 120 }, w => AmountColumnWidth = w);
        RegisterColumn("Note", new ColumnDef { StarValue = 1.0, MinWidth = 120, PreferredWidth = 150 }, w => NoteColumnWidth = w);

        RecalculateWidths();
    }
}
