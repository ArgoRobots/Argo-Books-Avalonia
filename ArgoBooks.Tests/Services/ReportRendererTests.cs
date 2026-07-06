using System.Reflection;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for ReportRenderer. CalculateGrowthRate is private (an internal step of summary rendering),
/// so it is invoked here via reflection.
/// </summary>
public class ReportRendererTests
{
    [Fact]
    public void CalculateGrowthRate_ComparesEqualLengthPeriods()
    {
        // Current period Jan 1-31. The previous comparison window must be the SAME length (Dec 1-31),
        // not one day shorter (Dec 2-31). With equal totals in both windows a correct comparison is 0%.
        var data = new CompanyData();
        data.Revenues.Add(new Revenue { Id = "C1", Date = new DateTime(2024, 1, 15), Total = 200m, OriginalCurrency = "USD" });
        // Previous-period revenue totalling the same $200; Dec 1 lands in the correct window but
        // not the buggy Dec 2-31 one.
        data.Revenues.Add(new Revenue { Id = "P1", Date = new DateTime(2023, 12, 1), Total = 100m, OriginalCurrency = "USD" });
        data.Revenues.Add(new Revenue { Id = "P2", Date = new DateTime(2023, 12, 15), Total = 100m, OriginalCurrency = "USD" });

        var config = new ReportConfiguration
        {
            Filters = new ReportFilters
            {
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 1, 31)
            }
        };

        using var renderer = new ReportRenderer(config, data);
        var summary = new SummaryReportElement { TransactionType = TransactionType.Revenue };

        var method = typeof(ReportRenderer).GetMethod("CalculateGrowthRate", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var growth = (double)method.Invoke(renderer, [summary])!;

        Assert.Equal(0d, growth);
    }
}
