using ArgoBooks.ViewModels.Dashboard;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// The "vs previous period" change compares against the period of the same length just before
/// the selected one. Ranges are stored as the start of their first day to the end of their last,
/// and the previous period came out a day short, so every change leaned upward.
/// </summary>
public class PreviousPeriodTests
{
    private static DateTime EndOf(DateTime day) => day.Date.AddDays(1).AddTicks(-1);

    [Fact]
    public void ThirtyDays_ComparesAgainstTheThirtyDaysBefore()
    {
        var (prevStart, prevEnd) = DashboardCalculations.PreviousPeriod(new DateTime(2026, 3, 1), EndOf(new DateTime(2026, 3, 30)));

        Assert.Equal(new DateTime(2026, 1, 30), prevStart);
        Assert.Equal(EndOf(new DateTime(2026, 2, 28)), prevEnd);
    }

    [Fact]
    public void OneDay_ComparesAgainstTheWholeDayBefore()
    {
        var (prevStart, prevEnd) = DashboardCalculations.PreviousPeriod(new DateTime(2026, 3, 1), EndOf(new DateTime(2026, 3, 1)));

        Assert.Equal(new DateTime(2026, 2, 28), prevStart);
        Assert.Equal(EndOf(new DateTime(2026, 2, 28)), prevEnd);
    }
}
