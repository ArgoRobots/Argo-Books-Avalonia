using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the AccountingReportDataService class.
/// </summary>
public class AccountingReportDataServiceTests
{
    private static ReportFilters CreateDefaultFilters() => new()
    {
        StartDate = new DateTime(2024, 1, 1),
        EndDate = new DateTime(2024, 12, 31)
    };

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCompanyData_CreatesInstance()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithEmptyCompanyData_CreatesInstance()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        Assert.NotNull(service);
    }

    #endregion

    #region Income Statement Tests

    [Fact]
    public void GetReportData_IncomeStatement_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.IncomeStatement);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_IncomeStatement_EmptyData_ReturnsValidResult()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.IncomeStatement);

        Assert.NotNull(result);
    }

    #endregion

    #region Balance Sheet Tests

    [Fact]
    public void GetReportData_BalanceSheet_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetReportData_BalanceSheet_WithInventory_AddsInventoryRow()
    {
        var data = new CompanyData();
        // 100 units @ $5 via a single manual Add adjustment dated mid-period.
        // No revenue/expense rows => Cash = 0 and AR = 0, so Total Current
        // Assets equals the inventory value alone.
        data.Inventory.Add(new InventoryItem { Id = "I1", InStock = 100, UnitCost = 5m });
        data.StockAdjustments.Add(new StockAdjustment
        {
            InventoryItemId = "I1",
            AdjustmentType = AdjustmentType.Add,
            Quantity = 100,
            PreviousStock = 0,
            NewStock = 100,
            Timestamp = new DateTime(2024, 6, 1)
        });
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        var inventoryRow = result.Rows.Find(r => r.Label == "Inventory");
        Assert.NotNull(inventoryRow);
        var totalCurrentAssetsRow = result.Rows.Find(r => r.Label == "Total Current Assets");
        Assert.NotNull(totalCurrentAssetsRow);
        // Cash and AR are zero, so the subtotal must equal the inventory value.
        Assert.Equal(inventoryRow!.Values[0], totalCurrentAssetsRow!.Values[0]);
    }

    [Fact]
    public void GetReportData_BalanceSheet_NoInventory_OmitsInventoryRow()
    {
        var data = new CompanyData();
        var service = new AccountingReportDataService(data, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.BalanceSheet);

        Assert.Null(result.Rows.Find(r => r.Label == "Inventory"));
    }

    #endregion

    #region Cash Flow Tests

    [Fact]
    public void GetReportData_CashFlow_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.CashFlowStatement);

        Assert.NotNull(result);
    }

    #endregion

    #region General Ledger Tests

    [Fact]
    public void GetReportData_GeneralLedger_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.GeneralLedger);

        Assert.NotNull(result);
    }

    #endregion

    #region AR/AP Aging Tests

    [Fact]
    public void GetReportData_ARAging_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.AccountsReceivableAging);

        Assert.NotNull(result);
    }

    #endregion

    #region Tax Summary Tests

    [Fact]
    public void GetReportData_TaxSummary_NullCompanyData_ReturnsValidResult()
    {
        var service = new AccountingReportDataService(null, CreateDefaultFilters());

        var result = service.GetReportData(AccountingReportType.TaxSummary);

        Assert.NotNull(result);
    }

    #endregion
}
