using ArgoBooks.Controls.Dashboard.Widgets;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.ViewModels.Dashboard;
using Avalonia.Controls;

namespace ArgoBooks.Services;

public record WidgetDefinition(
    WidgetType Type,
    string Name,
    string Description,
    string Category,
    string Icon,
    WidgetSize DefaultSize,
    WidgetSize[] AvailableSizes
);

public static class WidgetFactory
{
    private static readonly Dictionary<WidgetType, WidgetDefinition> Definitions = new()
    {
        [WidgetType.StatCardRevenue] = new(WidgetType.StatCardRevenue, "Revenue", "Total revenue for the period", "Statistics", "💰", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardExpenses] = new(WidgetType.StatCardExpenses, "Expenses", "Total expenses for the period", "Statistics", "📉", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardOutstandingInvoices] = new(WidgetType.StatCardOutstandingInvoices, "Outstanding Invoices", "Unpaid invoice total", "Statistics", "📋", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardActiveRentals] = new(WidgetType.StatCardActiveRentals, "Active Rentals", "Currently active rental count", "Statistics", "🔑", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardNetProfit] = new(WidgetType.StatCardNetProfit, "Net Profit", "Revenue minus expenses for the period", "Statistics", "📈", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardTotalCustomers] = new(WidgetType.StatCardTotalCustomers, "Total Customers", "Number of customers", "Statistics", "👥", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardInventoryValue] = new(WidgetType.StatCardInventoryValue, "Inventory Value", "Total value of inventory on hand", "Statistics", "📦", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.StatCardOverdueInvoices] = new(WidgetType.StatCardOverdueInvoices, "Overdue Invoices", "Invoices past their due date", "Statistics", "🚨", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small]),
        [WidgetType.QuickActions] = new(WidgetType.QuickActions, "Quick Actions", "Shortcut buttons for common tasks", "Actions", "⚡", WidgetSize.Large, [WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.ProfitsChart] = new(WidgetType.ProfitsChart, "Profits Chart", "Profit trends over time", "Charts", "📈", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.RevenueVsExpensesChart] = new(WidgetType.RevenueVsExpensesChart, "Revenue vs Expenses", "Compare revenue and expenses", "Charts", "📊", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.RecentTransactions] = new(WidgetType.RecentTransactions, "Recent Transactions", "Latest revenue and expense entries", "Tables", "📝", WidgetSize.Medium, [WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.ActiveRentalsTable] = new(WidgetType.ActiveRentalsTable, "Active Rentals Table", "Currently active and overdue rentals", "Tables", "📅", WidgetSize.Medium, [WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.SetupChecklist] = new(WidgetType.SetupChecklist, "Setup Checklist", "Getting started guide for new users", "Onboarding", "✅", WidgetSize.Large, [WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.TopCustomers] = new(WidgetType.TopCustomers, "Top Customers", "Highest revenue customers", "Insights", "👥", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium]),
        [WidgetType.LowStockAlerts] = new(WidgetType.LowStockAlerts, "Low Stock Alerts", "Inventory items below threshold", "Inventory", "⚠️", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium]),
        [WidgetType.UpcomingInvoiceDueDates] = new(WidgetType.UpcomingInvoiceDueDates, "Upcoming Due Dates", "Invoices due soon", "Invoices", "📆", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium]),
        [WidgetType.ExpenseByCategory] = new(WidgetType.ExpenseByCategory, "Expense by Category", "Expense breakdown by category", "Charts", "🍩", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large]),
        [WidgetType.OverdueRentals] = new(WidgetType.OverdueRentals, "Overdue Rentals", "Rentals past their due date", "Rentals", "🚨", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium]),
    };

    public static WidgetDefinition GetDefinition(WidgetType type) => Definitions[type];
    public static bool IsKnownType(WidgetType type) => Definitions.ContainsKey(type);
    public static IReadOnlyList<WidgetDefinition> GetAllDefinitions() => Definitions.Values.ToList();

    public static WidgetHostViewModel CreateWidgetHost(DashboardWidgetEntry entry)
    {
        var definition = GetDefinition(entry.WidgetType);
        var viewModel = CreateViewModel(entry.WidgetType);
        return new WidgetHostViewModel(entry, viewModel, definition.AvailableSizes);
    }

    public static WidgetViewModelBase CreateViewModel(WidgetType type) => type switch
    {
        WidgetType.StatCardRevenue => new StatCardWidgetViewModel(StatCardKind.Revenue),
        WidgetType.StatCardExpenses => new StatCardWidgetViewModel(StatCardKind.Expenses),
        WidgetType.StatCardOutstandingInvoices => new StatCardWidgetViewModel(StatCardKind.OutstandingInvoices),
        WidgetType.StatCardActiveRentals => new StatCardWidgetViewModel(StatCardKind.ActiveRentals),
        WidgetType.StatCardNetProfit => new StatCardWidgetViewModel(StatCardKind.NetProfit),
        WidgetType.StatCardTotalCustomers => new StatCardWidgetViewModel(StatCardKind.TotalCustomers),
        WidgetType.StatCardInventoryValue => new StatCardWidgetViewModel(StatCardKind.InventoryValue),
        WidgetType.StatCardOverdueInvoices => new StatCardWidgetViewModel(StatCardKind.OverdueInvoices),
        WidgetType.QuickActions => new QuickActionsWidgetViewModel(),
        WidgetType.ProfitsChart => new ChartWidgetViewModel(ChartWidgetKind.Profits),
        WidgetType.RevenueVsExpensesChart => new ChartWidgetViewModel(ChartWidgetKind.RevenueVsExpenses),
        WidgetType.RecentTransactions => new RecentTransactionsWidgetViewModel(),
        WidgetType.ActiveRentalsTable => new ActiveRentalsWidgetViewModel(),
        WidgetType.SetupChecklist => new SetupChecklistWidgetViewModel(),
        WidgetType.TopCustomers => new TopCustomersWidgetViewModel(),
        WidgetType.LowStockAlerts => new LowStockAlertsWidgetViewModel(),
        WidgetType.UpcomingInvoiceDueDates => new UpcomingInvoicesWidgetViewModel(),
        WidgetType.ExpenseByCategory => new ExpenseByCategoryWidgetViewModel(),
        WidgetType.OverdueRentals => new OverdueRentalsWidgetViewModel(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static Control CreateView(WidgetType type) => type switch
    {
        WidgetType.StatCardRevenue or
        WidgetType.StatCardExpenses or
        WidgetType.StatCardOutstandingInvoices or
        WidgetType.StatCardActiveRentals or
        WidgetType.StatCardNetProfit or
        WidgetType.StatCardTotalCustomers or
        WidgetType.StatCardInventoryValue or
        WidgetType.StatCardOverdueInvoices => new StatCardWidget(),
        WidgetType.QuickActions => new QuickActionsWidget(),
        WidgetType.ProfitsChart or
        WidgetType.RevenueVsExpensesChart => new ChartWidget(),
        WidgetType.RecentTransactions => new RecentTransactionsWidget(),
        WidgetType.ActiveRentalsTable => new ActiveRentalsWidget(),
        WidgetType.SetupChecklist => new SetupChecklistWidget(),
        WidgetType.TopCustomers => new TopCustomersWidget(),
        WidgetType.LowStockAlerts => new LowStockAlertsWidget(),
        WidgetType.UpcomingInvoiceDueDates => new UpcomingInvoicesWidget(),
        WidgetType.ExpenseByCategory => new ExpenseByCategoryWidget(),
        WidgetType.OverdueRentals => new OverdueRentalsWidget(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
