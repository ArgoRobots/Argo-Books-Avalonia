using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
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
