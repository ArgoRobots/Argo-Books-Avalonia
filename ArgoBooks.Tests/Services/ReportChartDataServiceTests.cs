using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ReportChartDataService class.
/// </summary>
public class ReportChartDataServiceTests
{
    private static ReportFilters CreateDefaultFilters() => new()
    {
        StartDate = new DateTime(2024, 1, 1),
        EndDate = new DateTime(2024, 12, 31)
    };

    #region Revenue Chart Tests

    [Fact]
    public void GetRevenueOverTime_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetRevenueOverTime();

        Assert.Empty(result);
    }

    [Fact]
    public void GetRevenueOverTime_EmptyRevenues_ReturnsEmptyList()
    {
        var data = new CompanyData();
        var service = new ReportChartDataService(data, CreateDefaultFilters());

        var result = service.GetRevenueOverTime();

        Assert.Empty(result);
    }

    [Fact]
    public void GetRevenueOverTime_WithData_ReturnsGroupedByDate()
    {
        var data = new CompanyData();
        data.Revenues.Add(new Revenue
        {
            Id = "R1",
            Date = new DateTime(2024, 6, 1),
            Subtotal = 100m,
            Total = 100m
        });
        data.Revenues.Add(new Revenue
        {
            Id = "R2",
            Date = new DateTime(2024, 6, 1),
            Subtotal = 200m,
            Total = 200m
        });
        var service = new ReportChartDataService(data, CreateDefaultFilters());

        var result = service.GetRevenueOverTime();

        Assert.Single(result); // Grouped by date
    }

    [Fact]
    public void GetRevenueDistribution_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetRevenueDistribution();

        Assert.Empty(result);
    }

    [Fact]
    public void GetTotalRevenue_NullCompanyData_ReturnsZero()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetTotalRevenue();

        Assert.Equal(0m, result);
    }

    #endregion

    #region Expense Chart Tests

    [Fact]
    public void GetExpensesOverTime_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetExpensesOverTime();

        Assert.Empty(result);
    }

    [Fact]
    public void GetExpenseDistribution_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetExpenseDistribution();

        Assert.Empty(result);
    }

    [Fact]
    public void GetTotalExpenses_NullCompanyData_ReturnsZero()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetTotalExpenses();

        Assert.Equal(0m, result);
    }

    #endregion

    #region Profit Chart Tests

    [Fact]
    public void GetProfitOverTime_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetProfitOverTime();

        Assert.Empty(result);
    }

    #endregion

    #region Combined Series Tests

    [Fact]
    public void GetRevenueVsExpenses_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetRevenueVsExpenses();

        Assert.Empty(result);
    }

    #endregion

    #region Customer Chart Tests

    [Fact]
    public void GetTopCustomersByRevenue_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetTopCustomersByRevenue();

        Assert.Empty(result);
    }

    [Fact]
    public void GetCustomerGrowth_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetCustomerGrowth();

        Assert.Empty(result);
    }

    #endregion

    #region Returns/Losses Chart Tests

    [Fact]
    public void GetReturnsOverTime_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetReturnsOverTime();

        Assert.Empty(result);
    }

    [Fact]
    public void GetLossesOverTime_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetLossesOverTime();

        Assert.Empty(result);
    }

    #endregion

    #region Geography Chart Tests

    [Fact]
    public void GetWorldMapData_NullCompanyData_ReturnsEmptyDictionary()
    {
        var service = new ReportChartDataService(null, CreateDefaultFilters());

        var result = service.GetWorldMapData();

        Assert.Empty(result);
    }

    #endregion

    #region Partial-month clamping

    [Fact]
    public void GetAverageTransactionValueBySeries_ClampsPartialMonthToFilterWindow()
    {
        // A sale inside the filter window and another in the same calendar month but outside it.
        // GetRevenueVsExpenses clamps month bounds to the window; this method did not, so the
        // out-of-window sale leaks into the average. (Revenue defaults to PaymentStatus.Paid.)
        var data = new CompanyData();
        data.Revenues.Add(new Revenue { Id = "R1", Date = new DateTime(2024, 1, 10), Total = 100m, OriginalCurrency = "USD" });
        data.Revenues.Add(new Revenue { Id = "R2", Date = new DateTime(2024, 1, 25), Total = 1000m, OriginalCurrency = "USD" });

        var filters = new ReportFilters { StartDate = new DateTime(2024, 1, 5), EndDate = new DateTime(2024, 1, 15) };
        var series = new ReportChartDataService(data, filters).GetAverageTransactionValueBySeries();

        var revenue = series.First(s => s.Name == "Revenue");
        // Only the Jan 10 sale ($100) is within Jan 5-15; the Jan 25 sale must be excluded.
        Assert.Equal(100d, revenue.DataPoints.Single().Value);
    }

    #endregion
}
