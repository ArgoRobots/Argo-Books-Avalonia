using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Bank Matching "Bank lines" table.
/// Columns: Date | Description | Amount | Status | Matched book entry | Actions
/// </summary>
public partial class BankLinesTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _dateColumnWidth = 110;

    [ObservableProperty]
    private double _descriptionColumnWidth = 220;

    [ObservableProperty]
    private double _amountColumnWidth = 110;

    [ObservableProperty]
    private double _statusColumnWidth = 110;

    [ObservableProperty]
    private double _matchedColumnWidth = 200;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public BankLinesTableColumnWidths()
    {
        ColumnOrder = ["Date", "Description", "Amount", "Status", "Matched", "Actions"];

        RegisterColumn("Date", new ColumnDef { StarValue = 0.8, MinWidth = 90, PreferredWidth = 110 }, w => DateColumnWidth = w);
        RegisterColumn("Description", new ColumnDef { StarValue = 2.0, MinWidth = 160, PreferredWidth = 220 }, w => DescriptionColumnWidth = w);
        RegisterColumn("Amount", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => AmountColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => StatusColumnWidth = w);
        RegisterColumn("Matched", new ColumnDef { StarValue = 1.6, MinWidth = 140, PreferredWidth = 200 }, w => MatchedColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = ActionsWidth(3), MinWidth = ActionsWidth(3) }, w => ActionsColumnWidth = w);

        RecalculateWidths();
    }
}
