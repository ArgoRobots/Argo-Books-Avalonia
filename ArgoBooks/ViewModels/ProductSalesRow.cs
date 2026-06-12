using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Row in the Analytics "Sales by Product" table. Wraps a
/// <see cref="ProductSalesData"/> and exposes display-currency strings for
/// binding, plus the raw USD figures for sorting. Currency conversion happens
/// here, at the presentation boundary (see docs/Calculations.md §13).
/// </summary>
public partial class ProductSalesRow : ObservableObject
{
    public ProductSalesRow(ProductSalesData data, DateTime displayDate)
    {
        ProductId = data.ProductId;
        ProductName = data.ProductName;
        Sku = data.Sku;

        UnitsSold = data.UnitsSold;
        RevenueUSD = data.RevenueUSD;
        AvgSalePriceUSD = data.AvgSalePriceUSD;

        UnitsDisplay = data.UnitsSold.ToString("0.##");
        RevenueDisplay = CurrencyService.FormatFromUSD(data.RevenueUSD, displayDate);
        AvgSalePriceDisplay = CurrencyService.FormatFromUSD(data.AvgSalePriceUSD, displayDate);
    }

    public string ProductId { get; }
    public string ProductName { get; }
    public string Sku { get; }

    // Raw USD values for sorting.
    public decimal UnitsSold { get; }
    public decimal RevenueUSD { get; }
    public decimal AvgSalePriceUSD { get; }

    // Display-currency strings for binding.
    public string UnitsDisplay { get; }
    public string RevenueDisplay { get; }
    public string AvgSalePriceDisplay { get; }

    /// <summary>True when the product has a SKU worth displaying.</summary>
    public bool HasSku => !string.IsNullOrEmpty(Sku);

    /// <summary>Highlights the row when it is the one shown in the detail panel.</summary>
    [ObservableProperty]
    private bool _isSelected;
}
