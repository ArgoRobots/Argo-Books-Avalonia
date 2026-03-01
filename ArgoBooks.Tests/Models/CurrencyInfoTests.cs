using ArgoBooks.Core.Models.Common;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the CurrencyInfo model.
/// </summary>
public class CurrencyInfoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var info = new CurrencyInfo("CAD", "$", "Canadian Dollar", 2);

        Assert.Equal("CAD", info.Code);
        Assert.Equal("$", info.Symbol);
        Assert.Equal("Canadian Dollar", info.Name);
        Assert.Equal(2, info.DecimalPlaces);
    }

    [Fact]
    public void Constructor_DefaultDecimalPlaces_IsTwo()
    {
        var info = new CurrencyInfo("USD", "$", "US Dollar");

        Assert.Equal(2, info.DecimalPlaces);
    }

    #endregion

    #region Format Tests

    [Fact]
    public void Format_WithAmount_ReturnsSymbolAndAmount()
    {
        var info = new CurrencyInfo("USD", "$", "US Dollar");

        var result = info.Format(1234.56m);

        Assert.Equal("$1,234.56", result);
    }

    [Fact]
    public void Format_WithIncludeCode_AddsCodeSuffix()
    {
        var info = new CurrencyInfo("EUR", "\u20ac", "Euro");

        var result = info.Format(1234.56m, includeCode: true);

        Assert.Equal("\u20ac1,234.56 EUR", result);
    }

    [Fact]
    public void Format_ZeroDecimalPlaces_FormatsAsWholeNumber()
    {
        var info = new CurrencyInfo("JPY", "\u00a5", "Japanese Yen", 0);

        var result = info.Format(1234m);

        Assert.Equal("\u00a51,234", result);
    }

    [Fact]
    public void Format_WithoutIncludeCode_OmitsCode()
    {
        var info = new CurrencyInfo("GBP", "\u00a3", "British Pound");

        var result = info.Format(500.00m, includeCode: false);

        Assert.DoesNotContain("GBP", result);
    }

    #endregion

    #region DisplayString Tests

    [Fact]
    public void DisplayString_ReturnsCodeDashNameParenSymbol()
    {
        var info = new CurrencyInfo("USD", "$", "US Dollar");

        Assert.Equal("USD - US Dollar ($)", info.DisplayString);
    }

    [Fact]
    public void ToString_ReturnsDisplayString()
    {
        var info = new CurrencyInfo("EUR", "\u20ac", "Euro");

        Assert.Equal(info.DisplayString, info.ToString());
    }

    #endregion

    #region GetByCode Tests

    [Fact]
    public void GetByCode_ValidCode_ReturnsCorrectInfo()
    {
        var info = CurrencyInfo.GetByCode("EUR");

        Assert.Equal("EUR", info.Code);
        Assert.Equal("\u20ac", info.Symbol);
        Assert.Equal("Euro", info.Name);
    }

    [Fact]
    public void GetByCode_InvalidCode_ReturnsUsdFallback()
    {
        var info = CurrencyInfo.GetByCode("INVALID");

        Assert.Equal("USD", info.Code);
    }

    [Fact]
    public void GetByCode_NullCode_ReturnsUsdFallback()
    {
        var info = CurrencyInfo.GetByCode(null!);

        Assert.Equal("USD", info.Code);
    }

    [Fact]
    public void GetByCode_EmptyCode_ReturnsUsdFallback()
    {
        var info = CurrencyInfo.GetByCode(string.Empty);

        Assert.Equal("USD", info.Code);
    }

    [Fact]
    public void GetByCode_CaseInsensitive_ReturnsCorrectInfo()
    {
        var info = CurrencyInfo.GetByCode("eur");

        Assert.Equal("EUR", info.Code);
    }

    #endregion

    #region ParseCodeFromDisplayString Tests

    [Fact]
    public void ParseCodeFromDisplayString_ValidDisplayString_ExtractsCode()
    {
        var code = CurrencyInfo.ParseCodeFromDisplayString("USD - US Dollar ($)");

        Assert.Equal("USD", code);
    }

    [Fact]
    public void ParseCodeFromDisplayString_JustCode_ReturnsUppercase()
    {
        var code = CurrencyInfo.ParseCodeFromDisplayString("eur");

        Assert.Equal("EUR", code);
    }

    [Fact]
    public void ParseCodeFromDisplayString_Null_ReturnsUsd()
    {
        var code = CurrencyInfo.ParseCodeFromDisplayString(null!);

        Assert.Equal("USD", code);
    }

    [Fact]
    public void ParseCodeFromDisplayString_Empty_ReturnsUsd()
    {
        var code = CurrencyInfo.ParseCodeFromDisplayString(string.Empty);

        Assert.Equal("USD", code);
    }

    #endregion

    #region GetSymbol Tests

    [Theory]
    [InlineData("USD", "$")]
    [InlineData("EUR", "\u20ac")]
    [InlineData("GBP", "\u00a3")]
    [InlineData("JPY", "\u00a5")]
    public void GetSymbol_ReturnsCorrectSymbol(string code, string expectedSymbol)
    {
        var symbol = CurrencyInfo.GetSymbol(code);

        Assert.Equal(expectedSymbol, symbol);
    }

    #endregion

    #region All Dictionary Tests

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CAD")]
    [InlineData("AUD")]
    public void All_ContainsExpectedCurrencies(string code)
    {
        Assert.True(CurrencyInfo.All.ContainsKey(code));
    }

    #endregion

    #region PriorityCodes Tests

    [Fact]
    public void PriorityCodes_ContainsExpectedCodes()
    {
        Assert.Contains("USD", CurrencyInfo.PriorityCodes);
        Assert.Contains("EUR", CurrencyInfo.PriorityCodes);
        Assert.Contains("CAD", CurrencyInfo.PriorityCodes);
        Assert.Contains("AUD", CurrencyInfo.PriorityCodes);
        Assert.Contains("GBP", CurrencyInfo.PriorityCodes);
    }

    [Fact]
    public void PriorityCodes_HasFiveEntries()
    {
        Assert.Equal(5, CurrencyInfo.PriorityCodes.Count);
    }

    #endregion

    #region Decimal Places Tests

    [Fact]
    public void JPY_HasZeroDecimalPlaces()
    {
        var jpy = CurrencyInfo.GetByCode("JPY");

        Assert.Equal(0, jpy.DecimalPlaces);
    }

    [Fact]
    public void USD_HasTwoDecimalPlaces()
    {
        var usd = CurrencyInfo.GetByCode("USD");

        Assert.Equal(2, usd.DecimalPlaces);
    }

    #endregion
}
