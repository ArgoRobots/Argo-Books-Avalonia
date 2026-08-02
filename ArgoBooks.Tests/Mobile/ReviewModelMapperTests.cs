using ArgoBooks.Core.Services;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Shared.Mobile;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>Unit tests for ReviewModelMapper's supplier/product auto-suggest and the
/// review-model -> CapturedTransaction build step.</summary>
public class ReviewModelMapperTests
{
    private static MobileSnapshot BuildSnapshot() => new()
    {
        Suppliers =
        [
            new RowDto { Title = "Home Depot", Subtitle = "Contact", Amount = "$0" },
        ],
        Products =
        [
            new RowDto { Title = "Lumber", Subtitle = "SKU 1", Amount = "0" },
            new RowDto { Title = "Hardware", Subtitle = "SKU 2", Amount = "0" },
        ],
    };

    private static ReceiptScanResult BuildResult(string? supplierName, params ScannedLineItem[] lineItems) => new()
    {
        IsSuccess = true,
        SupplierName = supplierName,
        TransactionDate = new DateTime(2026, 7, 8),
        TotalAmount = 84.12m,
        TaxAmount = 7.64m,
        LineItems = lineItems.ToList(),
    };

    [Fact]
    public void Map_MatchesSupplier_CaseInsensitively()
    {
        var result = BuildResult("home depot");
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        Assert.Equal("Home Depot", reviewModel.SupplierOrCustomer);
    }

    [Fact]
    public void Map_UnmatchedSupplier_KeepsRawScannedName()
    {
        var result = BuildResult("Some Unknown Vendor");
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        Assert.Equal("Some Unknown Vendor", reviewModel.SupplierOrCustomer);
    }

    [Fact]
    public void Map_DefaultsTypeToExpense()
    {
        var result = BuildResult("Home Depot");
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        Assert.Equal(CapturedTransactionType.Expense, reviewModel.Type);
    }

    [Fact]
    public void Map_LineItem_MatchingDescription_AutoSuggestsProduct()
    {
        var result = BuildResult("Home Depot", new ScannedLineItem { Description = "Lumber", Quantity = 2, UnitPrice = 6.20m, TotalPrice = 12.40m });
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        var line = Assert.Single(reviewModel.LineItems);
        Assert.Equal("Lumber", line.ProductName);
        Assert.True(line.IsMatched);
    }

    [Fact]
    public void Map_LineItem_FuzzyMatchesDescription_ContainingProductName()
    {
        var result = BuildResult("Home Depot", new ScannedLineItem { Description = "2x4 Lumber - 8ft", Quantity = 1, UnitPrice = 6.20m, TotalPrice = 6.20m });
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        var line = Assert.Single(reviewModel.LineItems);
        Assert.Equal("Lumber", line.ProductName);
        Assert.True(line.IsMatched);
    }

    [Fact]
    public void Map_LineItem_NoMatch_SuggestsFromDescription_NeverEmpty()
    {
        var result = BuildResult("Home Depot", new ScannedLineItem { Description = "Sandpaper pack", Quantity = 1, UnitPrice = 5.49m, TotalPrice = 5.49m });
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        var line = Assert.Single(reviewModel.LineItems);
        Assert.False(line.IsMatched);
        Assert.False(string.IsNullOrWhiteSpace(line.ProductName));
        Assert.Equal("Sandpaper pack", line.ProductName);
    }

    [Fact]
    public void Map_NoSnapshot_StillProducesNonEmptySuggestions()
    {
        var result = BuildResult("Home Depot", new ScannedLineItem { Description = "Utility knife", Quantity = 1, UnitPrice = 14.61m, TotalPrice = 14.61m });
        var reviewModel = ReviewModelMapper.Map(result, snapshot: null);

        Assert.Equal("Home Depot", reviewModel.SupplierOrCustomer);
        var line = Assert.Single(reviewModel.LineItems);
        Assert.False(string.IsNullOrWhiteSpace(line.ProductName));
    }

    [Fact]
    public void BuildCapturedTransaction_SetsFreshNonEmptyScanUid()
    {
        var reviewModel = ReviewModelMapper.Map(BuildResult("Home Depot", new ScannedLineItem { Description = "Lumber", Quantity = 1, UnitPrice = 6.20m, TotalPrice = 6.20m }), BuildSnapshot());

        var tx1 = ReviewModelMapper.BuildCapturedTransaction(reviewModel, imageBase64: null);
        var tx2 = ReviewModelMapper.BuildCapturedTransaction(reviewModel, imageBase64: null);

        Assert.False(string.IsNullOrWhiteSpace(tx1.ScanUid));
        Assert.NotEqual(tx1.ScanUid, tx2.ScanUid);
    }

    [Fact]
    public void BuildCapturedTransaction_CarriesTotalsAndLineItems()
    {
        var result = BuildResult(
            "Home Depot",
            new ScannedLineItem { Description = "Lumber", Quantity = 2, UnitPrice = 6.20m, TotalPrice = 12.40m },
            new ScannedLineItem { Description = "Wood screws", Quantity = 1, UnitPrice = 8.99m, TotalPrice = 8.99m });
        var reviewModel = ReviewModelMapper.Map(result, BuildSnapshot());

        var tx = ReviewModelMapper.BuildCapturedTransaction(reviewModel, imageBase64: "abc123");

        Assert.Equal(84.12m, tx.Total);
        Assert.Equal(7.64m, tx.Tax);
        Assert.Equal("Home Depot", tx.SupplierOrCustomer);
        Assert.Equal("abc123", tx.ImageBase64);
        Assert.Equal(2, tx.LineItems.Count);
        Assert.Equal("Lumber", tx.LineItems[0].ProductName);
        Assert.Equal(12.40m, tx.LineItems[0].Total);
    }
}
