using ArgoBooks.Controls;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
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
                IconGeometry = Geometry.Parse(Icons.Cash);
                IconColor = StatCardColor.Danger;
                break;
            case StatCardKind.Revenue:
                Label = "Total Revenue";
                IconGeometry = Geometry.Parse(Icons.DollarCircle);
                IconColor = StatCardColor.Success;
                break;
            case StatCardKind.OutstandingInvoices:
                Label = "Outstanding Invoices";
                IconGeometry = Geometry.Parse(Icons.Reports);
                IconColor = StatCardColor.Warning;
                break;
            case StatCardKind.ActiveRentals:
                Label = "Active Rentals";
                IconGeometry = Geometry.Parse(Icons.Bookmark);
                IconColor = StatCardColor.Info;
                break;
            case StatCardKind.NetProfit:
                Label = "Net Profit";
                IconGeometry = Geometry.Parse(Icons.TrendUp);
                IconColor = StatCardColor.Success;
                break;
            case StatCardKind.TotalCustomers:
                Label = "Total Customers";
                IconGeometry = Geometry.Parse(Icons.Customers);
                IconColor = StatCardColor.Info;
                break;
            case StatCardKind.InventoryValue:
                Label = "Inventory Value";
                IconGeometry = Geometry.Parse(Icons.Warehouse);
                IconColor = StatCardColor.Warning;
                break;
            case StatCardKind.OverdueInvoices:
                Label = "Overdue Invoices";
                IconGeometry = Geometry.Parse(Icons.WarningCircle);
                IconColor = StatCardColor.Danger;
                break;
        }
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
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
        var grossUSD = data.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.EffectiveSubtotalUSD);
        var refundsUSD = RefundAggregator.GetRefundedInDateRangeUSD(data.Payments, startDate, endDate);
        var currentUSD = grossUSD - refundsUSD;
        Value = CurrencyService.FormatFromUSD(currentUSD, DateTime.Now);

        var (prevStart, prevEnd) = DashboardCalculations.GetComparisonPeriod();
        if (prevStart != DateTime.MinValue && DashboardCalculations.HasSufficientPriorData(data, prevStart))
        {
            var prevGrossUSD = data.Revenues
                .Where(s => s.Date >= prevStart && s.Date <= prevEnd)
                .Sum(s => s.EffectiveSubtotalUSD);
            var prevRefundsUSD = RefundAggregator.GetRefundedInDateRangeUSD(data.Payments, prevStart, prevEnd);
            var prevUSD = prevGrossUSD - prevRefundsUSD;
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
        var refundsUSD = RefundAggregator.GetRefundedInDateRangeUSD(data.Payments, startDate, endDate);
        var expenseUSD = data.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .Sum(p => p.EffectiveSubtotalUSD);
        var profitUSD = revenueUSD - refundsUSD - expenseUSD;
        Value = CurrencyService.FormatFromUSD(profitUSD, DateTime.Now);

        var (prevStart, prevEnd) = DashboardCalculations.GetComparisonPeriod();
        if (prevStart != DateTime.MinValue && DashboardCalculations.HasSufficientPriorData(data, prevStart))
        {
            var prevRevenue = data.Revenues
                .Where(s => s.Date >= prevStart && s.Date <= prevEnd)
                .Sum(s => s.EffectiveSubtotalUSD);
            var prevRefunds = RefundAggregator.GetRefundedInDateRangeUSD(data.Payments, prevStart, prevEnd);
            var prevExpense = data.Expenses
                .Where(p => p.Date >= prevStart && p.Date <= prevEnd)
                .Sum(p => p.EffectiveSubtotalUSD);
            var prevProfit = prevRevenue - prevRefunds - prevExpense;
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
