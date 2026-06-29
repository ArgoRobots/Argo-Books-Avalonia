using ArgoBooks.Core.Models.Common;
using Xunit;

namespace ArgoBooks.Tests.Models;

/// <summary>
/// Tests for the symbol -> codes reverse index on <see cref="CurrencyInfo"/>, which drives
/// ambiguity detection (a symbol shared by several currencies is ambiguous).
/// </summary>
public class CurrencyInfoReverseLookupTests
{
    [Fact]
    public void CodesBySymbol_Dollar_HasThreeCodes_UsdFirst()
    {
        var codes = CurrencyInfo.CandidatesForSymbol("$");

        Assert.Contains("USD", codes);
        Assert.Contains("CAD", codes);
        Assert.Contains("AUD", codes);
        Assert.Equal("USD", codes[0]); // priority-ordered
    }

    [Fact]
    public void CodesBySymbol_Yen_HasTwoCodes()
    {
        var codes = CurrencyInfo.CandidatesForSymbol("¥");
        Assert.Equal(2, codes.Count);
        Assert.Contains("JPY", codes);
        Assert.Contains("CNY", codes);
    }

    [Fact]
    public void CodesBySymbol_Kr_HasFourCodes()
    {
        Assert.Equal(4, CurrencyInfo.CandidatesForSymbol("kr").Count); // DKK, ISK, NOK, SEK
    }

    [Theory]
    [InlineData("£", "GBP")]
    [InlineData("€", "EUR")]
    [InlineData("₹", "INR")]
    public void TryResolveSymbol_Unambiguous_ReturnsSingleCode(string symbol, string expected)
    {
        Assert.True(CurrencyInfo.TryResolveSymbol(symbol, out var code));
        Assert.Equal(expected, code);
        Assert.False(CurrencyInfo.CandidatesForSymbol(symbol).Count > 1);
    }

    [Theory]
    [InlineData("$")]
    [InlineData("¥")]
    [InlineData("kr")]
    public void SharedSymbols_AreAmbiguous(string symbol)
    {
        Assert.True(CurrencyInfo.CandidatesForSymbol(symbol).Count > 1);
        Assert.False(CurrencyInfo.TryResolveSymbol(symbol, out _));
    }

    [Fact]
    public void CandidatesForSymbol_UnknownSymbol_IsEmpty()
    {
        Assert.Empty(CurrencyInfo.CandidatesForSymbol("@@@"));
    }
}
