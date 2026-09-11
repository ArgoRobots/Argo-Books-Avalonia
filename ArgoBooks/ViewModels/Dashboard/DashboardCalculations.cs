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
            // Each previous period ends a tick before the current one starts, not at midnight on its
            // last day, so a refund or payment timed during that day is still counted.
            DateRangePreset.ThisMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1), new DateTime(now.Year, now.Month, 1).AddTicks(-1)),
            DateRangePreset.LastMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-2), new DateTime(now.Year, now.Month, 1).AddMonths(-1).AddTicks(-1)),
            DateRangePreset.Last30Days => (startDate.AddDays(-30), startDate.AddTicks(-1)),
            DateRangePreset.Last100Days => (startDate.AddDays(-100), startDate.AddTicks(-1)),
            DateRangePreset.Last365Days => (startDate.AddDays(-365), startDate.AddTicks(-1)),
            DateRangePreset.ThisQuarter => (new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3), new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddTicks(-1)),
            DateRangePreset.LastQuarter => (new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-6), new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(-3).AddTicks(-1)),
            DateRangePreset.ThisYear => (new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year, 1, 1).AddTicks(-1)),
            DateRangePreset.LastYear => (new DateTime(now.Year - 2, 1, 1), new DateTime(now.Year - 1, 1, 1).AddTicks(-1)),
            DateRangePreset.AllTime => (DateTime.MinValue, DateTime.MinValue),
            DateRangePreset.CustomRange => PreviousPeriod(startDate, endDate),
            _ => (startDate.AddDays(-30), startDate.AddTicks(-1))
        };
    }

    /// <summary>
    /// The period of the same number of days immediately before [start, end], for "vs previous
    /// period" figures. Ranges run from the start of their first day to the end of their last, so
    /// the previous one ends a tick before this one starts. Taking end - start as the length and
    /// ending at midnight on the day before made it a day short. MinValue when the range reaches
    /// back too far to have one, as All Time does.
    /// </summary>
    public static (DateTime Start, DateTime End) PreviousPeriod(DateTime start, DateTime end)
    {
        if (start <= DateTime.MinValue)
            return (DateTime.MinValue, DateTime.MinValue);

        var days = (end.Date - start.Date).Days + 1;
        var prevStart = (start.Date - DateTime.MinValue).TotalDays > days
            ? start.Date.AddDays(-days)
            : DateTime.MinValue;
        return (prevStart, start.AddTicks(-1));
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
