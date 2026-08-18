using System.Globalization;
using ArgoBooks.Behaviors;
using Xunit;

namespace ArgoBooks.Tests.Behaviors;

/// <summary>
/// Tests for reading a figure back out of a money box.
///
/// This parser used to try InvariantCulture first "because that is what the formatter writes".
/// Both halves were wrong: the money formatters write with CurrentCulture, and the style it used
/// allows thousands separators, so a comma-decimal string did not fail over to the second
/// attempt. It SUCCEEDED, reading the comma as grouping. On a French Canadian machine "16129,00"
/// parsed as 1612900, so opening an employee and pressing Save multiplied their TD1 claim by a
/// hundred, wiping their income tax withholding, and did it again on every later edit.
/// </summary>
public class CurrencyInputParsingTests
{
    private static decimal Parse(string text)
    {
        Assert.True(CurrencyInputBehavior.TryParse(text, out decimal value), $"failed to parse '{text}'");
        return value;
    }

    /// <summary>Runs a case under a specific machine locale, since the ambiguous ones depend on it.</summary>
    private static decimal ParseUnder(string culture, string text)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return Parse(text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>The bug, in the locale it bit: a French Canadian payroll is an ordinary one.</summary>
    [Theory]
    [InlineData("16129,00", 16129.00)]
    [InlineData("1,23", 1.23)]
    [InlineData("92000,50", 92000.50)]
    public void CommaDecimalIsNotReadAsGrouping(string text, double expected) =>
        Assert.Equal((decimal)expected, ParseUnder("fr-CA", text));

    /// <summary>
    /// And an invariant string still reads correctly on that same machine. Two decimals is the
    /// tell: no format groups by two, so it can only be a decimal point.
    /// </summary>
    [Fact]
    public void InvariantStringStillReadsOnACommaDecimalMachine() =>
        Assert.Equal(16129.00m, ParseUnder("fr-CA", "16129.00"));

    /// <summary>
    /// The genuinely ambiguous case, and the only one the machine's locale decides: exactly three
    /// digits after a single separator. A thousand to an English reader, 1.234 to a French one.
    /// </summary>
    [Fact]
    public void ThreeDigitTailFollowsTheMachineLocale()
    {
        Assert.Equal(1234m, ParseUnder("en-CA", "1,234"));
        Assert.Equal(1.234m, ParseUnder("fr-CA", "1,234"));
    }

    [Theory]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234,567.89", 1234567.89)]
    public void BothSeparatorsPresentMeansTheLastOneIsTheDecimalPoint(string text, double expected) =>
        Assert.Equal((decimal)expected, ParseUnder("en-CA", text));

    [Theory]
    [InlineData("$1,234.56", 1234.56)]
    [InlineData("-45.20", -45.20)]
    [InlineData("78000", 78000)]
    public void SymbolsAndSignsSurvive(string text, double expected) =>
        Assert.Equal((decimal)expected, ParseUnder("en-CA", text));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void NonNumbersAreRefused(string text) =>
        Assert.False(CurrencyInputBehavior.TryParse(text, out _));

    /// <summary>
    /// The round trip that actually corrupted the data: format a claim amount the way the
    /// employee form does, read it back, and it must be the same number on any machine.
    /// </summary>
    [Theory]
    [InlineData("fr-CA")]
    [InlineData("en-CA")]
    [InlineData("de-DE")]
    public void RoundTripIsStableUnderAnyLocale(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            const decimal claim = 16129.00m;
            string written = claim.ToString("0.00", CultureInfo.CurrentCulture);

            Assert.True(CurrencyInputBehavior.TryParse(written, out decimal read));
            Assert.Equal(claim, read);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
