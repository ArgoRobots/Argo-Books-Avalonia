using ArgoBooks.Core.Enums;
using SkiaSharp;

namespace ArgoBooks.Core;

/// <summary>
/// Semantic chart colors and the single source of truth for which color a
/// chart series should render in. Used by both in-app chart loaders and the
/// PDF report renderer so the dashboard, Analytics page, and exported reports
/// stay visually consistent.
/// </summary>
public static class ChartColors
{
    /// <summary>Money-in: revenue, profit, tax refund, etc.</summary>
    public static readonly SKColor Revenue = SKColor.Parse(AppColors.Success);

    /// <summary>Money-out: expense, loss, return, tax owed, shipping cost, etc.</summary>
    public static readonly SKColor Expense = SKColor.Parse(AppColors.ExpenseRed);

    /// <summary>Default brand blue for series with no money-in/money-out semantic.</summary>
    public static readonly SKColor Neutral = SKColor.Parse(AppColors.Primary);

    /// <summary>
    /// Picks the appropriate color for a single data point based on chart semantics.
    /// For split-aware chart types (TotalProfits, TaxLiabilityTrend) the value's sign
    /// chooses the color; flat semantic types ignore the value. Pass 0 to get the
    /// representative single-line color for split-aware types.
    /// </summary>
    public static SKColor ForValue(ChartDataType chartType, double value)
    {
        return chartType switch
        {
            // Money-out (expense family)
            ChartDataType.TotalExpenses or ChartDataType.ExpensesDistribution
                or ChartDataType.AverageShippingCosts
                or ChartDataType.ReturnsOverTime or ChartDataType.ReturnFinancialImpact
                or ChartDataType.LossesOverTime or ChartDataType.LossFinancialImpact
                => Expense,

            // Money-in (revenue family)
            ChartDataType.TotalRevenue or ChartDataType.RevenueDistribution
                => Revenue,

            // Profit: positive = green (profit), negative = red (loss)
            ChartDataType.TotalProfits
                => value >= 0 ? Revenue : Expense,

            // Net tax liability: positive = red (owe), negative = green (refund)
            ChartDataType.TaxLiabilityTrend
                => value >= 0 ? Expense : Revenue,

            _ => Neutral
        };
    }
}
