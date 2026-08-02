using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The Net Profit stat card sums <see cref="ProfitCalculator.CalculateNetProfitUSD"/>; the Profit
/// Over Time chart sums <see cref="ProfitCalculator.CalculateNetProfitByDayUSD"/>. They must agree,
/// or the card and the chart show different profit for the same data (the class of "surfaces
/// disagree" bug docs/Calculations.md exists to prevent). Profit is also cash-basis and pre-tax on
/// the revenue side (§2).
/// </summary>
public class ProfitReconciliationTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private static readonly DateTime End = new(2026, 12, 31);

    private static Revenue Rev(string id, DateTime date, decimal total, decimal tax, RevenuePaymentStatus status)
        => new()
        {
            Id = id,
            Date = date,
            OriginalCurrency = "USD",
            Total = total,
            TotalUSD = total,
            TaxAmount = tax,
            TaxAmountUSD = tax,
            PaymentStatus = status
        };

    private static Expense Exp(string id, DateTime date, decimal total)
        => new() { Id = id, Date = date, OriginalCurrency = "USD", Total = total, TotalUSD = total };

    private static CompanyData BuildCompany()
    {
        var data = new CompanyData();
        // Paid revenue: $100 gross, $10 tax -> $90 pre-tax counts toward profit.
        data.Revenues.Add(Rev("R1", new DateTime(2026, 3, 1), 100m, 10m, RevenuePaymentStatus.Paid));
        // Another paid revenue on a different day.
        data.Revenues.Add(Rev("R2", new DateTime(2026, 5, 15), 60m, 0m, RevenuePaymentStatus.Paid));
        // Unpaid revenue is excluded from cash-basis profit entirely.
        data.Revenues.Add(Rev("R3", new DateTime(2026, 6, 1), 500m, 50m, RevenuePaymentStatus.Pending));
        // Expenses on two days.
        data.Expenses.Add(Exp("E1", new DateTime(2026, 3, 1), 40m));
        data.Expenses.Add(Exp("E2", new DateTime(2026, 7, 20), 25m));
        return data;
    }

    [Fact]
    public void NetProfit_MatchesExpectedCashBasisPreTaxValue()
    {
        var data = BuildCompany();

        // (90 + 60) pre-tax paid revenue - (40 + 25) expenses = 85. Unpaid R3 excluded.
        Assert.Equal(85m, ProfitCalculator.CalculateNetProfitUSD(data, Start, End));
    }

    [Fact]
    public void CardTotal_EqualsChartByDaySum()
    {
        var data = BuildCompany();

        var cardTotal = ProfitCalculator.CalculateNetProfitUSD(data, Start, End);
        var chartSum = ProfitCalculator.CalculateNetProfitByDayUSD(data, Start, End).Values.Sum();

        Assert.Equal(cardTotal, chartSum);
    }

    [Fact]
    public void UnpaidRevenue_DoesNotAffectProfit()
    {
        var data = BuildCompany();
        var withUnpaid = ProfitCalculator.CalculateNetProfitUSD(data, Start, End);

        // Flip the unpaid revenue to a larger unpaid amount; profit must not move.
        data.Revenues.Add(Rev("R4", new DateTime(2026, 8, 1), 9999m, 999m, RevenuePaymentStatus.Overdue));

        Assert.Equal(withUnpaid, ProfitCalculator.CalculateNetProfitUSD(data, Start, End));
    }
}
