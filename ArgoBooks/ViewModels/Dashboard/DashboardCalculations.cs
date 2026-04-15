using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Services;

namespace ArgoBooks.ViewModels.Dashboard;

/// <summary>
/// Shared calculation helpers used by dashboard widgets and the main dashboard ViewModel.
/// </summary>
public static class DashboardCalculations
{
    public static (DateTime prevStart, DateTime prevEnd) GetComparisonPeriod()
    {
        var chartSettings = ChartSettingsService.Instance;
        var now = DateTime.Now;
        var preset = DateRangePresetExtensions.ParseDateRange(chartSettings.SelectedDateRange);
        var startDate = chartSettings.StartDate;
        var endDate = chartSettings.EndDate;

        return preset switch
        {
            DateRangePreset.ThisMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1).AddDays(-1)),
            DateRangePreset.LastMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-2), new DateTime(now.Year, now.Month, 1).AddMonths(-1).AddDays(-1)),
            DateRangePreset.Last30Days => (startDate.AddDays(-30), startDate.AddDays(-1)),
            DateRangePreset.Last100Days => (startDate.AddDays(-100), startDate.AddDays(-1)),
            DateRangePreset.Last365Days => (startDate.AddDays(-365), startDate.AddDays(-1)),
            DateRangePreset.ThisQuarter => (new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3), new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddDays(-1)),
            DateRangePreset.LastQuarter => (new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-6), new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3).AddDays(-1)),
            DateRangePreset.ThisYear => (new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year - 1, 12, 31)),
            DateRangePreset.LastYear => (new DateTime(now.Year - 2, 1, 1), new DateTime(now.Year - 2, 12, 31)),
            DateRangePreset.AllTime => (DateTime.MinValue, DateTime.MinValue),
            DateRangePreset.CustomRange => (startDate.AddDays(-(endDate - startDate).TotalDays - 1), startDate.AddDays(-1)),
            _ => (startDate.AddDays(-30), startDate.AddDays(-1))
        };
    }

    public static bool HasSufficientPriorData(CompanyData data, DateTime prevStartDate)
    {
        var earliestRevenue = data.Revenues.Count > 0 ? data.Revenues.Min(r => r.Date) : DateTime.MaxValue;
        var earliestExpense = data.Expenses.Count > 0 ? data.Expenses.Min(e => e.Date) : DateTime.MaxValue;
        var earliestDate = earliestRevenue < earliestExpense ? earliestRevenue : earliestExpense;
        return earliestDate != DateTime.MaxValue && earliestDate <= prevStartDate;
    }

    public static double? CalculatePercentageChange(decimal previous, decimal current)
    {
        if (previous == 0) return null;
        return (double)((current - previous) / previous * 100);
    }

    public static string? FormatPercentageChange(double? change)
    {
        if (!change.HasValue) return null;
        return $"{Math.Abs(change.Value):F1}%";
    }
}
