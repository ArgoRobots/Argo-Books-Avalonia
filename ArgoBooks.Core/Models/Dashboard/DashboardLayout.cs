namespace ArgoBooks.Core.Models.Dashboard;

public class DashboardLayout
{
    public List<DashboardRow> Rows { get; set; } = [];

    [JsonPropertyName("Widgets")]
    public List<DashboardWidgetEntry>? LegacyWidgets { get; set; }

    public void MigrateIfNeeded()
    {
        if (LegacyWidgets is { Count: > 0 } && Rows.Count == 0)
        {
            Rows = MigrateToRows(LegacyWidgets);
            LegacyWidgets = null;
        }
    }

    private static List<DashboardRow> MigrateToRows(List<DashboardWidgetEntry> widgets)
    {
        var rows = new List<DashboardRow>();
        var currentRow = new DashboardRow();
        double rowSum = 0;

        foreach (var widget in widgets)
        {
            var fraction = widget.Size.ToFraction();
            bool isFirst = rows.Count == 0 && currentRow.Widgets.Count == 0;
            bool startsNew = !isFirst && (widget.StartsNewRow || rowSum + fraction > 1.001);

            if (startsNew)
            {
                rows.Add(currentRow);
                currentRow = new DashboardRow();
                rowSum = 0;
            }

            currentRow.Widgets.Add(widget);
            rowSum += fraction;
        }

        if (currentRow.Widgets.Count > 0)
            rows.Add(currentRow);

        return rows;
    }

    public static DashboardLayout CreateDefault()
    {
        return new DashboardLayout
        {
            Rows =
            [
                new DashboardRow(
                    new DashboardWidgetEntry(WidgetType.StatCardRevenue, WidgetSize.Tiny),
                    new DashboardWidgetEntry(WidgetType.StatCardExpenses, WidgetSize.Tiny),
                    new DashboardWidgetEntry(WidgetType.StatCardOutstandingInvoices, WidgetSize.Tiny),
                    new DashboardWidgetEntry(WidgetType.StatCardActiveRentals, WidgetSize.Tiny)),
                new DashboardRow(
                    new DashboardWidgetEntry(WidgetType.SetupChecklist, WidgetSize.Large)),
                new DashboardRow(
                    new DashboardWidgetEntry(WidgetType.QuickActions, WidgetSize.Large)),
                new DashboardRow(
                    new DashboardWidgetEntry(WidgetType.Chart, WidgetSize.Medium)
                        { Config = new() { ["ChartDataType"] = "TotalProfits" } },
                    new DashboardWidgetEntry(WidgetType.Chart, WidgetSize.Medium)
                        { Config = new() { ["ChartDataType"] = "RevenueVsExpenses" } }),
                new DashboardRow(
                    new DashboardWidgetEntry(WidgetType.RecentTransactions, WidgetSize.Medium),
                    new DashboardWidgetEntry(WidgetType.ActiveRentalsTable, WidgetSize.Medium)),
            ]
        };
    }

    public DashboardLayout Clone() => new()
    {
        Rows = Rows.Select(r => r.Clone()).ToList()
    };
}
