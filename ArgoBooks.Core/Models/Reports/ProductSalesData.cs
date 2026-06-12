namespace ArgoBooks.Core.Models.Reports;

/// <summary>
/// Per-product sales for a date range, all USD-normalized. Produced by
/// <see cref="Services.ProductSalesService"/> and consumed by the Analytics
/// "Products" tab and the "Sales by Product" report. Revenue is gross
/// (tax-inclusive), matching every other revenue figure in the app
/// (docs/Calculations.md Rule 1). Display-currency conversion happens at the
/// presentation layer, never here. See docs/Calculations.md §13.
/// </summary>
public class ProductSalesData
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    /// <summary>Total units sold across the period.</summary>
    public decimal UnitsSold { get; set; }

    /// <summary>Gross revenue (USD), allocated proportionally across line items.</summary>
    public decimal RevenueUSD { get; set; }

    /// <summary>Average revenue per unit sold (USD) = revenue / units.</summary>
    public decimal AvgSalePriceUSD { get; set; }
}
