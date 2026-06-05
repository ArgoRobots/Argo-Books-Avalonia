namespace ArgoBooks.Core.Models.Dashboard;

public enum WidgetType
{
    StatCardRevenue,
    StatCardExpenses,
    StatCardOutstandingInvoices,
    StatCardActiveRentals,
    StatCardNetProfit,
    StatCardTotalCustomers,
    StatCardInventoryValue,
    StatCardOverdueInvoices,
    QuickActions,
    ProfitsChart,
    RevenueVsExpensesChart,
    RecentTransactions,
    ActiveRentalsTable,
    SetupChecklist,
    TopCustomers,
    LowStockAlerts,
    UpcomingInvoiceDueDates,
    ExpenseByCategory,
    // 18 was CashFlowSummary, removed, keep gap to avoid shifting values
    OverdueRentals = 19,
    Chart = 20
}
