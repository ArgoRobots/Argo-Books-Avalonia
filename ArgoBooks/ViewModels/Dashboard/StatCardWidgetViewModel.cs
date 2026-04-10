using ArgoBooks.Controls;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels.Dashboard;

public enum StatCardKind
{
    Revenue,
    Expenses,
    OutstandingInvoices,
    ActiveRentals,
    NetProfit,
    TotalCustomers,
    InventoryValue,
    OverdueInvoices
}

public partial class StatCardWidgetViewModel : WidgetViewModelBase
{
    public StatCardKind Kind { get; }

    public override WidgetType WidgetType => Kind switch
    {
        StatCardKind.Revenue => WidgetType.StatCardRevenue,
        StatCardKind.Expenses => WidgetType.StatCardExpenses,
        StatCardKind.OutstandingInvoices => WidgetType.StatCardOutstandingInvoices,
        StatCardKind.ActiveRentals => WidgetType.StatCardActiveRentals,
        StatCardKind.NetProfit => WidgetType.StatCardNetProfit,
        StatCardKind.TotalCustomers => WidgetType.StatCardTotalCustomers,
        StatCardKind.InventoryValue => WidgetType.StatCardInventoryValue,
        StatCardKind.OverdueInvoices => WidgetType.StatCardOverdueInvoices,
        _ => WidgetType.StatCardRevenue
    };

    [ObservableProperty]
    private string _label = "";

    [ObservableProperty]
    private string _value = "$0.00";

    [ObservableProperty]
    private Geometry? _iconGeometry;

    [ObservableProperty]
    private StatCardColor _iconColor = StatCardColor.Primary;

    [ObservableProperty]
    private double? _changeValue;

    [ObservableProperty]
    private string? _changeText;

    [ObservableProperty]
    private string _changeLabel = "";

    [ObservableProperty]
    private string? _secondaryText;

    public StatCardWidgetViewModel(StatCardKind kind)
    {
        Kind = kind;
        SetStaticProperties();
    }

    private void SetStaticProperties()
    {
        switch (Kind)
        {
            case StatCardKind.Expenses:
                Label = "Total Expenses";
                IconGeometry = Geometry.Parse("M19 14V6c0-1.1-.9-2-2-2H3c-1.1 0-2 .9-2 2v8c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zm-9-1c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm13-6v11c0 1.1-.9 2-2 2H4v-2h17V7h2z");
                IconColor = StatCardColor.Danger;
                break;
            case StatCardKind.Revenue:
                Label = "Total Revenue";
                IconGeometry = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1.41 16.09V20h-2.67v-1.93c-1.71-.36-3.16-1.46-3.27-3.4h1.96c.1 1.05.82 1.87 2.65 1.87 1.96 0 2.4-.98 2.4-1.59 0-.83-.44-1.61-2.67-2.14-2.48-.6-4.18-1.62-4.18-3.67 0-1.72 1.39-2.84 3.11-3.21V4h2.67v1.95c1.86.45 2.79 1.86 2.85 3.39H14.3c-.05-1.11-.64-1.87-2.22-1.87-1.5 0-2.4.68-2.4 1.64 0 .84.65 1.39 2.67 1.91s4.18 1.39 4.18 3.91c-.01 1.83-1.38 2.83-3.12 3.16z");
                IconColor = StatCardColor.Success;
                break;
            case StatCardKind.OutstandingInvoices:
                Label = "Outstanding Invoices";
                IconGeometry = Geometry.Parse("M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z");
                IconColor = StatCardColor.Warning;
                break;
            case StatCardKind.ActiveRentals:
                Label = "Active Rentals";
                IconGeometry = Geometry.Parse("M17 3H7c-1.1 0-2 .9-2 2v16l7-3 7 3V5c0-1.1-.9-2-2-2z");
                IconColor = StatCardColor.Info;
                break;
            case StatCardKind.NetProfit:
                Label = "Net Profit";
                IconGeometry = Geometry.Parse("M16 6l2.29 2.29-4.88 4.88-4-4L2 16.59 3.41 18l6-6 4 4 6.3-6.29L22 12V6z");
                IconColor = StatCardColor.Success;
                break;
            case StatCardKind.TotalCustomers:
                Label = "Total Customers";
                IconGeometry = Geometry.Parse("M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z");
                IconColor = StatCardColor.Info;
                break;
            case StatCardKind.InventoryValue:
                Label = "Inventory Value";
                IconGeometry = Geometry.Parse("M20 2H4c-1 0-2 1-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V5c0-1-1-2-2-2zm-5 12H9v-2h6v2zm5-7H4V5h16v2z");
                IconColor = StatCardColor.Warning;
                break;
            case StatCardKind.OverdueInvoices:
                Label = "Overdue Invoices";
                IconGeometry = Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z");
                IconColor = StatCardColor.Danger;
                break;
        }
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        System.Diagnostics.Debug.WriteLine($"[StatCard:{Kind}] LoadData: CompanyManager={CompanyManager != null}, CompanyData={data != null}, Revenues={data?.Revenues?.Count}, Expenses={data?.Expenses?.Count}");
        if (data == null) return;

        var chartSettings = ChartSettingsService.Instance;
        var startDate = chartSettings.StartDate;
        var endDate = chartSettings.EndDate;
        ChangeLabel = chartSettings.ComparisonPeriodLabel;

        switch (Kind)
        {
            case StatCardKind.Revenue:
                LoadRevenue(data, startDate, endDate);
                break;
            case StatCardKind.Expenses:
                LoadExpenses(data, startDate, endDate);
                break;
            case StatCardKind.OutstandingInvoices:
                LoadOutstandingInvoices(data);
                break;
            case StatCardKind.ActiveRentals:
                LoadActiveRentals(data);
                break;
            case StatCardKind.NetProfit:
                LoadNetProfit(data, startDate, endDate);
                break;
            case StatCardKind.TotalCustomers:
                LoadTotalCustomers(data);
                break;
            case StatCardKind.InventoryValue:
                LoadInventoryValue(data);
                break;
            case StatCardKind.OverdueInvoices:
                LoadOverdueInvoices(data);
                break;
        }
    }

    private void LoadRevenue(CompanyData data, DateTime startDate, DateTime endDate)
    {
        var currentUSD = data.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.EffectiveSubtotalUSD);
        Value = CurrencyService.FormatFromUSD(currentUSD, DateTime.Now);

        var (prevStart, prevEnd) = DashboardCalculations.GetComparisonPeriod();
        if (prevStart != DateTime.MinValue && DashboardCalculations.HasSufficientPriorData(data, prevStart))
        {
            var prevUSD = data.Revenues
                .Where(s => s.Date >= prevStart && s.Date <= prevEnd)
                .Sum(s => s.EffectiveSubtotalUSD);
            ChangeValue = DashboardCalculations.CalculatePercentageChange(prevUSD, currentUSD);
            ChangeText = DashboardCalculations.FormatPercentageChange(ChangeValue);
        }
        else
        {
            ChangeValue = null;
            ChangeText = null;
        }
    }

    private void LoadExpenses(CompanyData data, DateTime startDate, DateTime endDate)
    {
        var currentUSD = data.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.EffectiveSubtotalUSD);
        Value = CurrencyService.FormatFromUSD(currentUSD, DateTime.Now);

        var (prevStart, prevEnd) = DashboardCalculations.GetComparisonPeriod();
        if (prevStart != DateTime.MinValue && DashboardCalculations.HasSufficientPriorData(data, prevStart))
        {
            var prevUSD = data.Expenses
                .Where(p => p.Date >= prevStart && p.Date <= prevEnd)
                .Sum(p => p.EffectiveSubtotalUSD);
            ChangeValue = DashboardCalculations.CalculatePercentageChange(prevUSD, currentUSD);
            ChangeText = DashboardCalculations.FormatPercentageChange(ChangeValue);
        }
        else
        {
            ChangeValue = null;
            ChangeText = null;
        }
    }

    private void LoadOutstandingInvoices(CompanyData data)
    {
        var unpaid = data.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid
                     && i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Draft)
            .ToList();
        Value = CurrencyService.FormatFromUSD(unpaid.Sum(i => i.EffectiveBalanceUSD), DateTime.Now);
        SecondaryText = $"{unpaid.Count} invoices pending";
    }

    private void LoadActiveRentals(CompanyData data)
    {
        var active = data.Rentals
            .Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Overdue)
            .ToList();
        Value = active.Count.ToString();
        var overdue = active.Count(r => r.Status == RentalStatus.Overdue);
        SecondaryText = overdue > 0 ? $"{overdue} overdue" : null;
    }

    private void LoadNetProfit(CompanyData data, DateTime startDate, DateTime endDate)
    {
        var revenueUSD = data.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.EffectiveSubtotalUSD);
        var expenseUSD = data.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.EffectiveSubtotalUSD);
        var profitUSD = revenueUSD - expenseUSD;
        Value = CurrencyService.FormatFromUSD(profitUSD, DateTime.Now);

        var (prevStart, prevEnd) = DashboardCalculations.GetComparisonPeriod();
        if (prevStart != DateTime.MinValue && DashboardCalculations.HasSufficientPriorData(data, prevStart))
        {
            var prevRevenue = data.Revenues
                .Where(s => s.Date >= prevStart && s.Date <= prevEnd)
                .Sum(s => s.EffectiveSubtotalUSD);
            var prevExpense = data.Expenses
                .Where(p => p.Date >= prevStart && p.Date <= prevEnd)
                .Sum(p => p.EffectiveSubtotalUSD);
            var prevProfit = prevRevenue - prevExpense;
            ChangeValue = DashboardCalculations.CalculatePercentageChange(prevProfit, profitUSD);
            ChangeText = DashboardCalculations.FormatPercentageChange(ChangeValue);
        }
        else
        {
            ChangeValue = null;
            ChangeText = null;
        }
    }

    private void LoadTotalCustomers(CompanyData data)
    {
        Value = data.Customers.Count.ToString();
    }

    private void LoadInventoryValue(CompanyData data)
    {
        var totalValue = data.Inventory.Sum(i => i.TotalValue);
        Value = CurrencyService.Format(totalValue);
        var lowStock = data.Inventory.Count(i => i.InStock <= i.ReorderPoint && i.InStock > 0);
        SecondaryText = lowStock > 0 ? $"{lowStock} low stock" : $"{data.Inventory.Count} items";
    }

    private void LoadOverdueInvoices(CompanyData data)
    {
        var overdue = data.Invoices
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .ToList();
        Value = CurrencyService.FormatFromUSD(overdue.Sum(i => i.EffectiveBalanceUSD), DateTime.Now);
        SecondaryText = $"{overdue.Count} overdue";
    }

}
