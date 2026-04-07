namespace ArgoBooks.Core.Models.Dashboard;

public class DashboardLayout
{
    public List<DashboardWidgetEntry> Widgets { get; set; } = [];

    public static DashboardLayout CreateDefault() => new()
    {
        Widgets =
        [
            new(WidgetType.StatCardRevenue, WidgetSize.Tiny),
            new(WidgetType.StatCardExpenses, WidgetSize.Tiny),
            new(WidgetType.StatCardOutstandingInvoices, WidgetSize.Tiny),
            new(WidgetType.StatCardActiveRentals, WidgetSize.Tiny),
            new(WidgetType.SetupChecklist, WidgetSize.Large),
            new(WidgetType.QuickActions, WidgetSize.Large),
            new(WidgetType.ProfitsChart, WidgetSize.Medium),
            new(WidgetType.RevenueVsExpensesChart, WidgetSize.Medium),
            new(WidgetType.RecentTransactions, WidgetSize.Medium),
            new(WidgetType.ActiveRentalsTable, WidgetSize.Medium),
        ]
    };

    public DashboardLayout Clone() => new()
    {
        Widgets = Widgets.Select(w => w.Clone()).ToList()
    };
}
