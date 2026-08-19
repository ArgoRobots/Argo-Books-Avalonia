using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Controls.ColumnWidths;

/// <summary>
/// Manages column widths for the Pay runs table.
/// Columns: Pay date | Period | Employees | Gross | Net | Status | Actions
/// </summary>
public partial class PayRunsTableColumnWidths : TableColumnWidthsBase
{
    [ObservableProperty]
    private double _payDateColumnWidth = 120;

    [ObservableProperty]
    private double _periodColumnWidth = 200;

    [ObservableProperty]
    private double _employeesColumnWidth = 100;

    [ObservableProperty]
    private double _grossColumnWidth = 130;

    [ObservableProperty]
    private double _netColumnWidth = 130;

    [ObservableProperty]
    private double _statusColumnWidth = 110;

    [ObservableProperty]
    private double _actionsColumnWidth = 120;

    public PayRunsTableColumnWidths()
    {
        ColumnOrder = ["PayDate", "Period", "Employees", "Gross", "Net", "Status", "Actions"];

        RegisterColumn("PayDate", new ColumnDef { StarValue = 0.8, MinWidth = 100, PreferredWidth = 120 }, w => PayDateColumnWidth = w);
        RegisterColumn("Period", new ColumnDef { StarValue = 1.5, MinWidth = 160, PreferredWidth = 200 }, w => PeriodColumnWidth = w);
        RegisterColumn("Employees", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 100 }, w => EmployeesColumnWidth = w);
        RegisterColumn("Gross", new ColumnDef { StarValue = 0.9, MinWidth = 110, PreferredWidth = 130 }, w => GrossColumnWidth = w);
        RegisterColumn("Net", new ColumnDef { StarValue = 0.9, MinWidth = 110, PreferredWidth = 130 }, w => NetColumnWidth = w);
        RegisterColumn("Status", new ColumnDef { StarValue = 0.7, MinWidth = 90, PreferredWidth = 110 }, w => StatusColumnWidth = w);
        RegisterColumn("Actions", new ColumnDef { IsFixed = true, FixedWidth = ActionsWidth(3), MinWidth = ActionsWidth(3) }, w => ActionsColumnWidth = w);
    }
}
