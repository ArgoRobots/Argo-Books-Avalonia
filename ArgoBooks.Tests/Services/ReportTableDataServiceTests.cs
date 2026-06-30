using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ReportTableDataService class.
/// </summary>
public class ReportTableDataServiceTests
{
    private static ReportFilters CreateDefaultFilters() => new()
    {
        StartDate = new DateTime(2024, 1, 1),
        EndDate = new DateTime(2024, 12, 31)
    };

    private static TableReportElement CreateDefaultTableConfig() => new()
    {
        MaxRows = 10,
        SortOrder = TableSortOrder.DateDescending,
        DataSelection = TableDataSelection.All
    };

    #region Revenue Table Tests

    [Fact]
    public void GetRevenueTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetRevenueTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    [Fact]
    public void GetRevenueTableData_EmptyRevenues_ReturnsEmptyList()
    {
        var data = new CompanyData();
        var service = new ReportTableDataService(data, CreateDefaultFilters());

        var result = service.GetRevenueTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Expense Table Tests

    [Fact]
    public void GetExpensesTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetExpensesTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Invoice Table Tests

    [Fact]
    public void GetInvoicesTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetInvoicesTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Payment Table Tests

    [Fact]
    public void GetPaymentsTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetPaymentsTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Inventory Table Tests

    [Fact]
    public void GetInventoryTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetInventoryTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Entity Table Tests

    [Fact]
    public void GetCustomersTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetCustomersTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    [Fact]
    public void GetSuppliersTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetSuppliersTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    [Fact]
    public void GetProductsTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetProductsTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Analysis Table Tests

    [Fact]
    public void GetTopProductsByRevenue_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetTopProductsByRevenue();

        Assert.Empty(result);
    }

    [Fact]
    public void GetTopCustomersByRevenue_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetTopCustomersByRevenue();

        Assert.Empty(result);
    }

    [Fact]
    public void GetTopSuppliersByVolume_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetTopSuppliersByVolume();

        Assert.Empty(result);
    }

    #endregion

    #region Returns/Losses Table Tests

    [Fact]
    public void GetReturnsTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetReturnsTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    [Fact]
    public void GetLossesTableData_NullCompanyData_ReturnsEmptyList()
    {
        var service = new ReportTableDataService(null, CreateDefaultFilters());

        var result = service.GetLossesTableData(CreateDefaultTableConfig());

        Assert.Empty(result);
    }

    #endregion

    #region Summary Statistics

    [Fact]
    public void GetSummaryStatistics_Expenses_UsesGrossAmountIncludingTax()
    {
        // The Dashboard and ProfitCalculator treat an expense's gross amount (including tax paid to
        // the supplier) as the cost that left the business. Summary Statistics used the pre-tax
        // subtotal instead, so its expense total (and the report's Net Profit) disagreed with the
        // Dashboard by exactly the supplier tax.
        var data = new CompanyData();
        data.Expenses.Add(new Expense
        {
            Id = "EXP-1",
            Date = new DateTime(2024, 6, 1),
            OriginalCurrency = "USD",
            Total = 550m,       // gross
            TaxAmount = 50m     // => pre-tax subtotal is 500
        });

        var filters = new ReportFilters
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            TransactionType = TransactionType.Expenses
        };

        var stats = new ReportTableDataService(data, filters).GetSummaryStatistics();

        Assert.Equal(550m, stats.TotalExpenses);
    }

    #endregion
}
