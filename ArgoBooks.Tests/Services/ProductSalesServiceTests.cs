using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for ProductSalesService: per-product gross revenue and units, allocated
/// per line item. Revenue is gross (EffectiveTotalUSD) per docs/Calculations.md
/// Rule 1 and §13; analytics is cash-basis, the report is accrual.
/// </summary>
public class ProductSalesServiceTests
{
    private static readonly DateTime Start = new(2026, 5, 1);
    private static readonly DateTime End = new(2026, 5, 31);

    private static Product Prod(string id, string name, string sku = "") =>
        new() { Id = id, Name = name, Sku = sku };

    private static LineItem Line(string productId, decimal qty, decimal unitPrice) =>
        new() { ProductId = productId, Quantity = qty, UnitPrice = unitPrice };

    private static Revenue PaidRevenue(decimal total, decimal tax, params LineItem[] lines) =>
        new()
        {
            Date = new DateTime(2026, 5, 11),
            PaymentStatus = RevenuePaymentStatus.Paid,
            Total = total,
            TaxAmount = tax,
            OriginalCurrency = "USD",
            LineItems = [.. lines]
        };

    [Fact]
    public void GetProductSales_SingleProduct_UsesGrossRevenue()
    {
        // $119 total ($86.91 + $32.09 tax). Revenue display is gross (Rule 1).
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget", "W-1"));
        data.Revenues.Add(PaidRevenue(119m, 32.09m, Line("P1", 2, 43.455m)));

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        var p = Assert.Single(result);
        Assert.Equal("Widget", p.ProductName);
        Assert.Equal("W-1", p.Sku);
        Assert.Equal(119m, p.RevenueUSD);          // gross, not the 86.91 pre-tax
        Assert.Equal(2m, p.UnitsSold);
        Assert.Equal(59.50m, p.AvgSalePriceUSD);   // 119 / 2
    }

    [Fact]
    public void GetProductSales_MultipleLineItems_AllocatesBySubtotalShare()
    {
        // One $200 sale split across two products 60/40 by line subtotal.
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Big"));
        data.Products.Add(Prod("P2", "Small"));
        data.Revenues.Add(PaidRevenue(200m, 0m,
            Line("P1", 1, 60m),
            Line("P2", 1, 40m)));

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        Assert.Equal(2, result.Count);
        Assert.Equal("Big", result[0].ProductName);   // ordered by revenue descending
        Assert.Equal(120m, result[0].RevenueUSD);      // 60/100 * 200
        Assert.Equal(80m, result[1].RevenueUSD);       // 40/100 * 200
    }

    [Fact]
    public void GetProductSales_CashBasis_ExcludesUnpaidButAccrualIncludes()
    {
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget"));
        data.Revenues.Add(PaidRevenue(100m, 0m, Line("P1", 1, 100m)));
        var pending = PaidRevenue(50m, 0m, Line("P1", 1, 50m));
        pending.PaymentStatus = RevenuePaymentStatus.Pending;
        data.Revenues.Add(pending);

        var cash = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);
        var accrual = ProductSalesService.GetProductSales(data, Start, End, cashBasis: false);

        Assert.Equal(100m, Assert.Single(cash).RevenueUSD);
        Assert.Equal(150m, Assert.Single(accrual).RevenueUSD);
    }

    [Fact]
    public void GetProductSales_NonUsd_UsesConvertedGrossUsd()
    {
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget"));
        var rev = PaidRevenue(100m, 0m, Line("P1", 1, 100m));
        rev.OriginalCurrency = "EUR";
        rev.TotalUSD = 110m;   // EffectiveTotalUSD for a non-USD transaction
        data.Revenues.Add(rev);

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        Assert.Equal(110m, Assert.Single(result).RevenueUSD);  // converted USD, not the 100 native
    }

    [Fact]
    public void GetProductSales_TransactionWithNoLineItems_IsExcluded()
    {
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget"));
        data.Revenues.Add(PaidRevenue(100m, 0m));   // no line items, nothing to attribute

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        Assert.Empty(result);
    }

    [Fact]
    public void GetProductSales_OutsideDateRange_IsExcluded()
    {
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget"));
        var rev = PaidRevenue(100m, 0m, Line("P1", 1, 100m));
        rev.Date = new DateTime(2026, 4, 15);  // before Start
        data.Revenues.Add(rev);

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        Assert.Empty(result);
    }

    [Fact]
    public void GetProductSales_UnknownProductId_GroupsAsUnknown()
    {
        var data = new CompanyData();   // no product registered for "GHOST"
        data.Revenues.Add(PaidRevenue(100m, 0m, Line("GHOST", 1, 100m)));

        var result = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true);

        Assert.Equal("Unknown", Assert.Single(result).ProductName);
    }

    [Fact]
    public void GetProductSales_EmptyData_ReturnsEmpty()
    {
        var result = ProductSalesService.GetProductSales(new CompanyData(), Start, End, cashBasis: true);
        Assert.Empty(result);
    }

    [Fact]
    public void GetProductRevenueByDayUSD_SumMatchesProductTotal()
    {
        var data = new CompanyData();
        data.Products.Add(Prod("P1", "Widget"));
        var r1 = PaidRevenue(100m, 0m, Line("P1", 1, 100m));
        r1.Date = new DateTime(2026, 5, 5);
        var r2 = PaidRevenue(60m, 0m, Line("P1", 1, 60m));
        r2.Date = new DateTime(2026, 5, 20);
        data.Revenues.Add(r1);
        data.Revenues.Add(r2);

        var byDay = ProductSalesService.GetProductRevenueByDayUSD(data, "P1", Start, End, cashBasis: true);
        var total = ProductSalesService.GetProductSales(data, Start, End, cashBasis: true).Single().RevenueUSD;

        Assert.Equal(2, byDay.Count);
        Assert.Equal(160m, byDay.Values.Sum());
        Assert.Equal(total, byDay.Values.Sum());   // per-day reconciles with the total
    }
}
