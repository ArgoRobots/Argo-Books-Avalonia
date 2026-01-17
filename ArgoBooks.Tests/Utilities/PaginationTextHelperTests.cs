using ArgoBooks.Utilities;
using Xunit;

namespace ArgoBooks.Tests.Utilities;

/// <summary>
/// Tests for the PaginationTextHelper class.
/// </summary>
public class PaginationTextHelperTests
{
    #region FormatPaginationText Tests

    [Fact]
    public void FormatPaginationText_ZeroItems_ReturnsZeroPlural()
    {
        var result = PaginationTextHelper.FormatPaginationText(0, 1, 10, 0, "customer", "customers");

        Assert.Equal("0 customers", result);
    }

    [Fact]
    public void FormatPaginationText_SingleItem_ReturnsSingular()
    {
        var result = PaginationTextHelper.FormatPaginationText(1, 1, 10, 1, "customer", "customers");

        Assert.Equal("1 customer", result);
    }

    [Fact]
    public void FormatPaginationText_MultipleItemsSinglePage_ReturnsCount()
    {
        var result = PaginationTextHelper.FormatPaginationText(5, 1, 10, 1, "customer", "customers");

        Assert.Equal("5 customers", result);
    }

    [Fact]
    public void FormatPaginationText_FirstPage_ReturnsCorrectRange()
    {
        var result = PaginationTextHelper.FormatPaginationText(25, 1, 10, 3, "customer", "customers");

        Assert.Equal("1-10 of 25 customers", result);
    }

    [Fact]
    public void FormatPaginationText_MiddlePage_ReturnsCorrectRange()
    {
        var result = PaginationTextHelper.FormatPaginationText(25, 2, 10, 3, "customer", "customers");

        Assert.Equal("11-20 of 25 customers", result);
    }

    [Fact]
    public void FormatPaginationText_LastPage_ReturnsCorrectRange()
    {
        var result = PaginationTextHelper.FormatPaginationText(25, 3, 10, 3, "customer", "customers");

        Assert.Equal("21-25 of 25 customers", result);
    }

    [Fact]
    public void FormatPaginationText_NullPlural_AddsS()
    {
        var result = PaginationTextHelper.FormatPaginationText(5, 1, 10, 1, "item");

        Assert.Equal("5 items", result);
    }

    [Fact]
    public void FormatPaginationText_NullPluralSingularCase_UsesSingular()
    {
        var result = PaginationTextHelper.FormatPaginationText(1, 1, 10, 1, "item");

        Assert.Equal("1 item", result);
    }

    [Theory]
    [InlineData(100, 1, 25, 4, "invoice", "invoices", "1-25 of 100 invoices")]
    [InlineData(100, 2, 25, 4, "invoice", "invoices", "26-50 of 100 invoices")]
    [InlineData(100, 3, 25, 4, "invoice", "invoices", "51-75 of 100 invoices")]
    [InlineData(100, 4, 25, 4, "invoice", "invoices", "76-100 of 100 invoices")]
    public void FormatPaginationText_VariousPages_ReturnsCorrectRange(
        int totalCount, int currentPage, int pageSize, int totalPages,
        string singular, string plural, string expected)
    {
        var result = PaginationTextHelper.FormatPaginationText(
            totalCount, currentPage, pageSize, totalPages, singular, plural);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatPaginationText_PartialLastPage_ReturnsCorrectEnd()
    {
        // 47 items, 10 per page, page 5 should show 41-47
        var result = PaginationTextHelper.FormatPaginationText(47, 5, 10, 5, "product", "products");

        Assert.Equal("41-47 of 47 products", result);
    }

    #endregion

    #region FormatSimpleCount Tests

    [Fact]
    public void FormatSimpleCount_ZeroItems_ReturnsZeroPlural()
    {
        var result = PaginationTextHelper.FormatSimpleCount(0, "receipt", "receipts");

        Assert.Equal("0 receipts", result);
    }

    [Fact]
    public void FormatSimpleCount_SingleItem_ReturnsSingular()
    {
        var result = PaginationTextHelper.FormatSimpleCount(1, "receipt", "receipts");

        Assert.Equal("1 receipt", result);
    }

    [Fact]
    public void FormatSimpleCount_MultipleItems_ReturnsPlural()
    {
        var result = PaginationTextHelper.FormatSimpleCount(42, "receipt", "receipts");

        Assert.Equal("42 receipts", result);
    }

    [Fact]
    public void FormatSimpleCount_NullPlural_AddsS()
    {
        var result = PaginationTextHelper.FormatSimpleCount(5, "order");

        Assert.Equal("5 orders", result);
    }

    [Fact]
    public void FormatSimpleCount_NullPluralSingular_UsesSingular()
    {
        var result = PaginationTextHelper.FormatSimpleCount(1, "order");

        Assert.Equal("1 order", result);
    }

    [Theory]
    [InlineData(0, "item", "0 items")]
    [InlineData(1, "item", "1 item")]
    [InlineData(2, "item", "2 items")]
    [InlineData(100, "item", "100 items")]
    [InlineData(1000, "item", "1000 items")]
    public void FormatSimpleCount_VariousCounts_ReturnsCorrectFormat(
        int count, string singular, string expected)
    {
        var result = PaginationTextHelper.FormatSimpleCount(count, singular);

        Assert.Equal(expected, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FormatPaginationText_SinglePageWithExactPageSize_ReturnsSimpleCount()
    {
        // 10 items on a page of 10 = 1 page total
        var result = PaginationTextHelper.FormatPaginationText(10, 1, 10, 1, "sale", "sales");

        Assert.Equal("10 sales", result);
    }

    [Fact]
    public void FormatPaginationText_LargeNumbers_FormatsCorrectly()
    {
        var result = PaginationTextHelper.FormatPaginationText(
            10000, 50, 100, 100, "transaction", "transactions");

        Assert.Equal("4901-5000 of 10000 transactions", result);
    }

    [Fact]
    public void FormatPaginationText_PageSizeOf1_WorksCorrectly()
    {
        var result = PaginationTextHelper.FormatPaginationText(5, 3, 1, 5, "item", "items");

        Assert.Equal("3-3 of 5 items", result);
    }

    #endregion

    #region Different Entity Names

    [Theory]
    [InlineData("customer", "customers")]
    [InlineData("invoice", "invoices")]
    [InlineData("product", "products")]
    [InlineData("employee", "employees")]
    [InlineData("category", "categories")]
    [InlineData("entry", "entries")]
    public void FormatSimpleCount_VariousEntityNames_FormatsCorrectly(string singular, string plural)
    {
        var resultSingle = PaginationTextHelper.FormatSimpleCount(1, singular, plural);
        var resultMultiple = PaginationTextHelper.FormatSimpleCount(5, singular, plural);

        Assert.Equal($"1 {singular}", resultSingle);
        Assert.Equal($"5 {plural}", resultMultiple);
    }

    #endregion
}
