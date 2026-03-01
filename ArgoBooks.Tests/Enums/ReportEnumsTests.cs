using ArgoBooks.Core.Enums;
using Xunit;

namespace ArgoBooks.Tests.Enums;

/// <summary>
/// Tests for report-related enum extension methods.
/// </summary>
public class ReportEnumsTests
{
    #region TransactionType Tests

    [Fact]
    public void TransactionType_AllGetDisplayName_ReturnNonEmptyStrings()
    {
        foreach (var value in Enum.GetValues<TransactionType>())
        {
            var displayName = value.GetDisplayName();

            Assert.False(string.IsNullOrEmpty(displayName),
                $"TransactionType.{value} returned null or empty display name.");
        }
    }

    [Theory]
    [InlineData(TransactionType.Revenue, "Revenue")]
    [InlineData(TransactionType.Expenses, "Expenses")]
    [InlineData(TransactionType.PurchaseOrders, "Purchase Orders")]
    [InlineData(TransactionType.LostDamaged, "Lost / Damaged")]
    public void TransactionType_GetDisplayName_ReturnsExpectedString(TransactionType type, string expected)
    {
        Assert.Equal(expected, type.GetDisplayName());
    }

    #endregion

    #region SupportsReturnsAndLosses Tests

    [Theory]
    [InlineData(TransactionType.Revenue)]
    [InlineData(TransactionType.Expenses)]
    public void SupportsReturnsAndLosses_RevenueAndExpenses_ReturnsTrue(TransactionType type)
    {
        Assert.True(type.SupportsReturnsAndLosses());
    }

    [Theory]
    [InlineData(TransactionType.Invoices)]
    [InlineData(TransactionType.Payments)]
    [InlineData(TransactionType.Customers)]
    [InlineData(TransactionType.Products)]
    [InlineData(TransactionType.PurchaseOrders)]
    [InlineData(TransactionType.Inventory)]
    public void SupportsReturnsAndLosses_OtherTypes_ReturnsFalse(TransactionType type)
    {
        Assert.False(type.SupportsReturnsAndLosses());
    }

    #endregion

    #region TableSortOrder Tests

    [Fact]
    public void TableSortOrder_AllGetDisplayName_ReturnNonEmptyStrings()
    {
        foreach (var value in Enum.GetValues<TableSortOrder>())
        {
            var displayName = value.GetDisplayName();

            Assert.False(string.IsNullOrEmpty(displayName),
                $"TableSortOrder.{value} returned null or empty display name.");
        }
    }

    [Theory]
    [InlineData(TableSortOrder.DateDescending, "Date descending")]
    [InlineData(TableSortOrder.AmountAscending, "Amount ascending")]
    public void TableSortOrder_GetDisplayName_ReturnsExpectedString(TableSortOrder order, string expected)
    {
        Assert.Equal(expected, order.GetDisplayName());
    }

    #endregion

    #region ChartDataType Tests

    [Fact]
    public void ChartDataType_AllGetDisplayName_ReturnNonEmptyStrings()
    {
        foreach (var value in Enum.GetValues<ChartDataType>())
        {
            var displayName = value.GetDisplayName();

            Assert.False(string.IsNullOrEmpty(displayName),
                $"ChartDataType.{value} returned null or empty display name.");
        }
    }

    [Theory]
    [InlineData(ChartDataType.TotalRevenue, "Revenue Trends")]
    [InlineData(ChartDataType.TotalExpenses, "Expense Trends")]
    [InlineData(ChartDataType.RevenueVsExpenses, "Expenses vs Revenue")]
    public void ChartDataType_GetDisplayName_ReturnsExpectedString(ChartDataType type, string expected)
    {
        Assert.Equal(expected, type.GetDisplayName());
    }

    #endregion

    #region AccountingReportType Tests

    [Fact]
    public void AccountingReportType_AllGetDisplayName_ReturnNonEmptyStrings()
    {
        foreach (var value in Enum.GetValues<AccountingReportType>())
        {
            var displayName = value.GetDisplayName();

            Assert.False(string.IsNullOrEmpty(displayName),
                $"AccountingReportType.{value} returned null or empty display name.");
        }
    }

    [Theory]
    [InlineData(AccountingReportType.IncomeStatement, "Income Statement")]
    [InlineData(AccountingReportType.BalanceSheet, "Balance Sheet")]
    [InlineData(AccountingReportType.TaxSummary, "Tax Summary")]
    public void AccountingReportType_GetDisplayName_ReturnsExpectedString(
        AccountingReportType type, string expected)
    {
        Assert.Equal(expected, type.GetDisplayName());
    }

    #endregion
}
