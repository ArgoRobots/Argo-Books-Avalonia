using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.ViewModels.Dashboard;

namespace ArgoBooks.Services;

public record WidgetDefinition(
    WidgetType Type,
    string Name,
    string Description,
    string Category,
    string Icon,
    WidgetSize DefaultSize,
    WidgetSize[] AvailableSizes,
    bool AllowDuplicates
);

public static class WidgetFactory
{
    private static readonly Dictionary<WidgetType, WidgetDefinition> Definitions = new()
    {
        [WidgetType.StatCardRevenue] = new(WidgetType.StatCardRevenue, "Revenue", "Total revenue for the period", "Statistics", "💰", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small], false),
        [WidgetType.StatCardExpenses] = new(WidgetType.StatCardExpenses, "Expenses", "Total expenses for the period", "Statistics", "📉", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small], false),
        [WidgetType.StatCardOutstandingInvoices] = new(WidgetType.StatCardOutstandingInvoices, "Outstanding Invoices", "Unpaid invoice total", "Statistics", "📋", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small], false),
        [WidgetType.StatCardActiveRentals] = new(WidgetType.StatCardActiveRentals, "Active Rentals", "Currently active rental count", "Statistics", "🔑", WidgetSize.Tiny, [WidgetSize.Tiny, WidgetSize.Small], false),
        [WidgetType.QuickActions] = new(WidgetType.QuickActions, "Quick Actions", "Shortcut buttons for common tasks", "Actions", "⚡", WidgetSize.Large, [WidgetSize.Medium, WidgetSize.Large], false),
        [WidgetType.ProfitsChart] = new(WidgetType.ProfitsChart, "Profits Chart", "Profit trends over time", "Charts", "📈", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large], true),
        [WidgetType.RevenueVsExpensesChart] = new(WidgetType.RevenueVsExpensesChart, "Revenue vs Expenses", "Compare revenue and expenses", "Charts", "📊", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large], true),
        [WidgetType.RecentTransactions] = new(WidgetType.RecentTransactions, "Recent Transactions", "Latest revenue and expense entries", "Tables", "📝", WidgetSize.Medium, [WidgetSize.Medium, WidgetSize.Large], false),
        [WidgetType.ActiveRentalsTable] = new(WidgetType.ActiveRentalsTable, "Active Rentals Table", "Currently active and overdue rentals", "Tables", "📅", WidgetSize.Medium, [WidgetSize.Medium, WidgetSize.Large], false),
        [WidgetType.SetupChecklist] = new(WidgetType.SetupChecklist, "Setup Checklist", "Getting started guide for new users", "Onboarding", "✅", WidgetSize.Large, [WidgetSize.Medium, WidgetSize.Large], false),
        [WidgetType.TopCustomers] = new(WidgetType.TopCustomers, "Top Customers", "Highest revenue customers", "Insights", "👥", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium], false),
        [WidgetType.LowStockAlerts] = new(WidgetType.LowStockAlerts, "Low Stock Alerts", "Inventory items below threshold", "Inventory", "⚠️", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium], false),
        [WidgetType.UpcomingInvoiceDueDates] = new(WidgetType.UpcomingInvoiceDueDates, "Upcoming Due Dates", "Invoices due soon", "Invoices", "📆", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium], false),
        [WidgetType.ExpenseByCategory] = new(WidgetType.ExpenseByCategory, "Expense by Category", "Expense breakdown by category", "Charts", "🍩", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large], true),
        [WidgetType.CashFlowSummary] = new(WidgetType.CashFlowSummary, "Cash Flow", "Income vs outflow summary", "Insights", "💵", WidgetSize.Medium, [WidgetSize.Small, WidgetSize.Medium, WidgetSize.Large], true),
        [WidgetType.OverdueRentals] = new(WidgetType.OverdueRentals, "Overdue Rentals", "Rentals past their due date", "Rentals", "🚨", WidgetSize.Small, [WidgetSize.Small, WidgetSize.Medium], false),
    };

    public static WidgetDefinition GetDefinition(WidgetType type) => Definitions[type];
    public static IReadOnlyList<WidgetDefinition> GetAllDefinitions() => Definitions.Values.ToList();
    public static IReadOnlyList<string> GetCategories() => Definitions.Values.Select(d => d.Category).Distinct().ToList();

    public static WidgetHostViewModel CreateWidgetHost(DashboardWidgetEntry entry)
    {
        var definition = GetDefinition(entry.WidgetType);
        var viewModel = CreateViewModel(entry.WidgetType);
        return new WidgetHostViewModel(entry, viewModel, definition.AvailableSizes);
    }

    // NOTE: Will be filled in as widget ViewModels are implemented in Tasks 4-10.
    public static WidgetViewModelBase CreateViewModel(WidgetType type)
    {
        // TODO: Will be filled as Tasks 4-10 are completed
        throw new NotImplementedException($"Widget ViewModel for {type} not yet implemented");
    }
}
