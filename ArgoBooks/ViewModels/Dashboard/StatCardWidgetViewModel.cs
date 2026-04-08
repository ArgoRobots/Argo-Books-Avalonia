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
    ActiveRentals
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

}
