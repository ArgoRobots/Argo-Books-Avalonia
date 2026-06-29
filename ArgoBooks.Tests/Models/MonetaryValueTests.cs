using ArgoBooks.Core.Models.Common;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the MonetaryValue model.
/// </summary>
public class MonetaryValueTests
{
    #region USD Constructor Tests

    [Fact]
    public void Constructor_UsdOnly_SetsAmountUSD()
    {
        var mv = new MonetaryValue(100.50m);

        Assert.Equal(100.50m, mv.AmountUSD);
    }

    [Fact]
    public void Constructor_UsdOnly_SetsOriginalAmountToSameValue()
    {
        var mv = new MonetaryValue(250m);

        Assert.Equal(250m, mv.OriginalAmount);
    }

    [Fact]
    public void Constructor_UsdOnly_SetsOriginalCurrencyToUSD()
    {
        var mv = new MonetaryValue(50m);

        Assert.Equal("USD", mv.OriginalCurrency);
    }

    #endregion

    #region Full Constructor Tests

    [Fact]
    public void Constructor_Full_SetsAllProperties()
    {
        var date = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var mv = new MonetaryValue(130m, "CAD", 100m, date);

        Assert.Equal(130m, mv.OriginalAmount);
        Assert.Equal("CAD", mv.OriginalCurrency);
        Assert.Equal(100m, mv.AmountUSD);
        Assert.Equal(date, mv.RateDate);
    }

    [Theory]
    [InlineData(100, "EUR", 110, "EUR")]
    [InlineData(1500, "JPY", 10, "JPY")]
    [InlineData(80, "GBP", 100, "GBP")]
    public void Constructor_Full_SupportsVariousCurrencies(
        decimal originalAmount, string currency, decimal amountUSD, string expectedCurrency)
    {
        var date = DateTime.UtcNow;
        var mv = new MonetaryValue(originalAmount, currency, amountUSD, date);

        Assert.Equal(originalAmount, mv.OriginalAmount);
        Assert.Equal(expectedCurrency, mv.OriginalCurrency);
        Assert.Equal(amountUSD, mv.AmountUSD);
    }

    #endregion

    #region TryGetDisplayAmount (exact-date) Tests

    [Fact]
    public void TryGetDisplayAmount_NoExactRate_ReturnsFalse()
    {
        var mv = new MonetaryValue(1200m, "GBP", 1611.40m, new DateTime(2026, 1, 5));

        var ok = mv.TryGetDisplayAmount("CAD", (_, _, _) => (false, 0m), out var result);

        Assert.False(ok);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TryGetDisplayAmount_OriginalCurrency_ReturnsExactOriginal()
    {
        var mv = new MonetaryValue(1200m, "GBP", 1611.40m, new DateTime(2026, 1, 5));

        var ok = mv.TryGetDisplayAmount("GBP", (_, _, _) => (false, 0m), out var result);

        Assert.True(ok);
        Assert.Equal(1200m, result);
    }

    [Fact]
    public void TryGetDisplayAmount_Usd_ReturnsAmountUsd()
    {
        var mv = new MonetaryValue(1200m, "GBP", 1611.40m, new DateTime(2026, 1, 5));

        var ok = mv.TryGetDisplayAmount("USD", (_, _, _) => (false, 0m), out var result);

        Assert.True(ok);
        Assert.Equal(1611.40m, result);
    }

    [Fact]
    public void TryGetDisplayAmount_ExactRate_Converts()
    {
        var mv = new MonetaryValue(1200m, "GBP", 1611.40m, new DateTime(2026, 1, 5));

        var ok = mv.TryGetDisplayAmount("CAD", (_, _, _) => (true, 2255.96m), out var result);

        Assert.True(ok);
        Assert.Equal(2255.96m, result);
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_FromDecimal_AssumesUSD()
    {
        MonetaryValue mv = 42.50m;

        Assert.Equal(42.50m, mv.AmountUSD);
        Assert.Equal(42.50m, mv.OriginalAmount);
        Assert.Equal("USD", mv.OriginalCurrency);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var mv = new MonetaryValue(130m, "CAD", 100m, DateTime.UtcNow);

        var result = mv.ToString();

        Assert.Equal("100.00 USD (original: 130.00 CAD)", result);
    }

    [Fact]
    public void ToString_UsdOnly_ShowsSameAmountTwice()
    {
        var mv = new MonetaryValue(50m);

        var result = mv.ToString();

        Assert.Equal("50.00 USD (original: 50.00 USD)", result);
    }

    #endregion
}
